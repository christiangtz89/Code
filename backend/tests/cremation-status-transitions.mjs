// Real API/SQL regression. NEVER use a development or production database.
// Migrate a fresh disposable pcms_c4a_* PostgreSQL database and start the API
// with an isolated signing key and issuer/audience c4a-qa. Set C4A_QA_CONTAINER
// (pcms-c4a-*), C4A_QA_DATABASE, C4A_QA_URL (loopback), C4A_QA_KEY and
// C4A_QA_STATE (temporary JSON). Run seed, then red on the baseline; migrate
// and restart the API, then verify. Dispose of the entire QA database afterwards.
// Removing either transition write, its transaction, actor binding or sequence
// protection must fail these observable API/database assertions.
import assert from 'node:assert/strict';
import { createHmac, randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C4A_QA_CONTAINER;
const database = process.env.C4A_QA_DATABASE;
const url = process.env.C4A_QA_URL;
const key = process.env.C4A_QA_KEY;
const statePath = process.env.C4A_QA_STATE;
assert.match(container ?? '', /^pcms-c4a-/);
assert.match(database ?? '', /^pcms_c4a_/);
assert.match(url ?? '', /^http:\/\/127\.0\.0\.1:\d+$/);
assert.ok(key && statePath, 'Explicit disposable signing key and state path required');

function sql(query) {
  return execFileSync('docker', ['exec', '-i', container, 'psql', '-X',
    '-v', 'ON_ERROR_STOP=1', '-U', 'postgres', '-d', database, '-At'],
  { input: query, encoding: 'utf8' }).trim();
}
const literal = value => value === null ? 'NULL' : `'${String(value).replaceAll("'", "''")}'`;
const insert = (table, values) => sql(`INSERT INTO "${table}" (${Object.keys(values).map(x => `"${x}"`).join(',')}) VALUES (${Object.values(values).map(literal).join(',')});`);
const rows = (table, where) => JSON.parse(sql(`SELECT coalesce(json_agg(t ORDER BY t."Id"),'[]') FROM (SELECT * FROM "${table}" WHERE ${where}) t;`));
const token = (actor, permissions = ['Cremations.View', 'Cremations.Manage']) => {
  const encode = object => Buffer.from(JSON.stringify(object)).toString('base64url');
  const data = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({
    sub: actor, iss: 'c4a-qa', aud: 'c4a-qa', exp: Math.floor(Date.now() / 1000) + 3600,
    permission: permissions,
  })}`;
  return `${data}.${createHmac('sha256', key).update(data).digest('base64url')}`;
};
async function api(path, method = 'GET', body, auth, expected = 200) {
  const response = await fetch(`${url}/api/Cremations/${path}`, {
    method, headers: { 'Content-Type': 'application/json', ...(auth ? { Authorization: `Bearer ${auth}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  assert.equal(response.status, expected, `${method} ${path}: ${text}`);
  return text ? JSON.parse(text) : null;
}


const historyTable = 'TransicionesEstadoCremacion';
if (process.argv[2] === 'seed') {
  assert.equal(sql('SELECT count(*) FROM "Cremaciones";'), '0');
  const s = { actor: randomUUID(), other: randomUUID(), inactive: randomUUID(),
    customer: randomUUID(), package: randomUUID(), request: randomUUID(), cases: {},
    scheduledAt: new Date(Math.floor(Date.now() / 3600000) * 3600000).toISOString() };
  const now = new Date().toISOString();
  for (const [id, name, active] of [[s.actor, 'Asignado', true], [s.other, 'Operador', true], [s.inactive, 'Inactivo', false]]) {
    insert('Usuarios', { Id: id, Nombre: name, Apellido: 'QA C4A',
      CorreoElectronico: `${id}@example.invalid`, HashContrasena: 'NOT_A_PASSWORD',
      Activo: active, EsOwner: false, FechaCreacion: now });
  }
  insert('Clientes', { Id: s.customer, Nombre: 'Cliente', ApellidoPaterno: 'C4A',
    Telefono: '000', CorreoElectronico: 'client@example.invalid', Activo: true, FechaCreacion: now });
  insert('PaquetesCremacion', { Id: s.package, Nombre: 'C4A QA', TipoPaquete: 1,
    IncluyeUrna: false, IncluyeHuella: false, IncluyeCertificado: false,
    EsPublico: false, OrdenVisualizacion: 0, Activo: true, FechaCreacion: now });
  for (const [name, status] of [['main',2], ['red',4], ['cancelNone',1], ['cancelComment',2],
    ['cancelWhitespace',2], ['put',1], ['race',4], ['atomic',5], ['legacy',6],
    ['inactive',4], ['patchSchedule',1], ['putRace',1], ['putAtomic',1]]) {
    const id = randomUUID(); const pet = randomUUID(); const reception = randomUUID();
    s.cases[name] = id;
    insert('Mascotas', { Id: pet, CustomerId: s.customer, Nombre: `QA ${name}`, Especie: 'Canino',
      Raza: 'QA', Sexo: 'M', Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-10-01', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: s.actor,
      FechaRecepcion: '2026-10-01T00:00:00Z', CodigoQr: `C4A-${name.toUpperCase()}`,
      PesoVerificadoKg: 5, TieneObjetosPersonales: false, Activo: true, FechaCreacion: now,
      NombreClienteSnapshot: 'Cliente C4A', NombreMascotaSnapshot: `QA ${name}` });
    insert('Cremaciones', { Id: id, RecepcionId: reception, AsignadoAUsuarioId: s.actor,
      TipoCremacion: 1, Estado: status, NombrePaquete: 'C4A QA', PaqueteCremacionId: s.package,
      IncluyeUrna: false, IncluyeHuella: false, IncluyeCertificado: false,
      FechaProgramada: name === 'main' ? s.scheduledAt : name === 'patchSchedule' ? '2026-12-01T16:00:00Z' : null,
      FechaInicio: name === 'legacy' ? '2026-10-02T10:00:00Z' : null,
      FechaFinalizacion: name === 'legacy' ? '2026-10-02T14:00:00Z' : null,
      Notas: 'Nota general original', PrecioCotizado: 300, MontoPagoRequeridoInicio: 0,
      PesoCotizadoKg: 5, PesoMinimoCotizadoKg: 0, PesoMaximoCotizadoKg: 5,
      Activo: true, FechaCreacion: now });
  }
  insert('ComentariosOperativosCremacion', { Id: randomUUID(), CremacionId: s.cases.legacy,
    Estado: 6, Comentario: 'Evidencia previa C3-A', CreadoPorUsuarioId: s.actor,
    NombreUsuarioSnapshot: 'Nombre histórico', FechaCreacion: '2026-10-02T14:00:00Z' });
  insert('VerificacionesInicioCremacion', { Id: randomUUID(), CremacionId: s.cases.legacy,
    RecepcionId: rows('Cremaciones', `"Id"=${literal(s.cases.legacy)}`)[0].RecepcionId,
    CodigoQrRecepcionSnapshot: 'C4A-LEGACY', ConfirmadoPorUsuarioId: s.actor,
    NombreUsuarioConfirmoSnapshot: 'Nombre histórico', FechaConfirmacion: '2026-10-02T10:00:00Z', SolicitudId: randomUUID() });
  writeFileSync(statePath, JSON.stringify(s, null, 2), { mode: 0o600 });
  console.log('Seed PASS: isolated actors, commercial fixtures and genuine pre-C4 evidence.');
} else {
  const s = JSON.parse(readFileSync(statePath, 'utf8'));
  const auth = token(s.other); const startAuth = token(s.actor);
  const id = s.cases.main;
  const current = id => rows('Cremaciones', `"Id"=${literal(id)}`)[0];
  const evidence = (table, id) => rows(table, `"CremacionId"=${literal(id)}`);
  const comments = id => evidence('ComentariosOperativosCremacion', id);
  const history = id => evidence(historyTable, id).sort((a,b) => a.Secuencia-b.Secuencia);
  const snapshot = id => ({ current: current(id), history: history(id), comments: comments(id),
    start: evidence('VerificacionesInicioCremacion', id) });
  const change = (id, body, expected = 200, credential = auth) => api(`${id}/status`, 'PATCH', body, credential, expected);
  const update = (id, scheduledAt, extra = {}, expected = 200, credential = auth) => api(id, 'PUT', {
    assignedToUserId: s.actor, cremationPackageId: s.package, urnId: null,
    accessoryDescription: null, scheduledAt, specialInstructions: null, notes: 'Nota general original', ...extra,
  }, credential, expected);
  if (process.argv[2] === 'red') {
    await change(s.cases.red, { status: 5 });
    // The baseline endpoint returns 404: the committed change has no readable transition evidence.
    const response = await fetch(`${url}/api/Cremations/${s.cases.red}/status-transitions`,
      { headers: { Authorization: `Bearer ${auth}` } });
    assert.equal(response.status, 200, 'Every committed transition must have readable immutable history');
    assert.equal((await response.json()).length, 1);
  } else {
    assert.equal(process.argv[2], 'verify');
    assert.equal(sql(`SELECT count(*) FROM "${historyTable}";`), '0', 'No migration backfill');
    const legacy = snapshot(s.cases.legacy);
    await api(s.cases.legacy, 'GET', undefined, auth);
    assert.deepEqual(await api(`${s.cases.legacy}/status-transitions`, 'GET', undefined, auth), []);
    assert.deepEqual(snapshot(s.cases.legacy), legacy);
    console.log('C4A-16 PASS: legacy readable, existing timestamps/comment/C1A unchanged; no backfill');

    function check(id, sequence, previous, next, actor, commentText = null, milestone = null) {
      const all = history(id); assert.equal(all.length, sequence);
      const row = all.at(-1);
      assert.equal(row.Secuencia, sequence); assert.equal(row.EstadoAnterior, previous);
      assert.equal(row.EstadoNuevo, next); assert.equal(row.ActorUsuarioId, actor);
      assert.equal(row.NombreActorSnapshot, actor === s.actor ? 'Asignado QA C4A' : 'Operador QA C4A');
      assert.ok(Number.isFinite(Date.parse(row.FechaOcurrencia)));
      assert.equal(current(id).Estado, next);
      if (milestone) assert.equal(row.FechaOcurrencia, current(id)[milestone]);
      if (commentText !== null) {
        const linked = comments(id).find(c => c.Id === row.ComentarioOperativoId);
        assert.ok(linked); assert.equal(linked.Comentario, commentText);
        assert.equal(linked.Estado, next); assert.equal(linked.CreadoPorUsuarioId, actor);
        assert.equal(linked.NombreUsuarioSnapshot, row.NombreActorSnapshot);
        assert.equal(linked.FechaCreacion, row.FechaOcurrencia);
      } else assert.equal(row.ComentarioOperativoId, null);
      return row;
    }
    const start = { status: 3, requestId: s.request, receptionQrCode: 'C4A-main', custodyAccepted: true,
      notes: 'Inicio C4A', actorUserId: s.other, actorNameSnapshot: 'FORGED' };
    await change(id, start, 200, startAuth);
    const first = check(id,1,2,3,s.actor,'Inicio C4A','FechaInicio');
    const verification = evidence('VerificacionesInicioCremacion',id);
    assert.equal(verification.length,1); assert.equal(verification[0].ConfirmadoPorUsuarioId,s.actor);
    assert.equal(verification[0].FechaConfirmacion,first.FechaOcurrencia);
    assert.equal(verification[0].SolicitudId,s.request);
    assert.equal(comments(id).length,1);
    console.log('C4A-1 PASS: coherent start/status/comment/C1A; request actor spoof ignored');
    for (const [num,next,previous,text,milestone] of [
      [2,4,3,undefined,null], [3,5,4,'Procesamiento C4A',null],
      [4,6,5,undefined,'FechaFinalizacion'], [5,7,6,'   ','FechaListaParaEntrega'],
      [6,8,7,'','FechaEntrega']]) {
      await change(id,{status:next,notes:text});
      check(id,num,previous,next,s.other,text?.trim() || null,milestone);
      assert.equal(comments(id).length,next < 5 ? 1 : 2);
      console.log(`C4A-${num} PASS: ${previous} -> ${next}, actor/time/sequence and commentary coherent`);
    }
    for (const [name,previous,text,num] of [['cancelNone',1,undefined,7],['cancelComment',2,'Cancelación C4A',8],['cancelWhitespace',2,'   ',7]]) {
      await change(s.cases[name],{status:9,notes:text});
      check(s.cases[name],1,previous,9,s.other,text?.trim() || null);
      assert.equal(comments(s.cases[name]).length,text?.trim() ? 1 : 0);
      console.log(`C4A-${num} PASS: ${name}`);
    }
    await update(s.cases.put,'2026-12-03T16:00:00Z',{actorUserId:s.actor,actorNameSnapshot:'FORGED'});
    check(s.cases.put,1,1,2,s.other); console.log('C4A-9 PASS: schedule PUT actor is authenticated caller');
    await update(s.cases.put,null); check(s.cases.put,2,2,1,s.other);
    console.log('C4A-10 PASS: schedule clear records Scheduled -> Pending');
    const noop = snapshot(s.cases.put);
    await update(s.cases.put,null); assert.deepEqual(snapshot(s.cases.put),noop);
    await update(s.cases.put,'2026-12-03T16:00:00Z');
    await update(s.cases.put,'2026-12-04T16:00:00Z');
    assert.equal(history(s.cases.put).length,3,'Same-status reschedule adds no transition');
    await update(s.cases.put,null); assert.equal(history(s.cases.put).length,4);
    console.log('C4A-11 PASS: no-op/same-status reschedule add no transition');
    const beforeReplay = snapshot(id);
    await change(id,start,200,startAuth); assert.deepEqual(snapshot(id),beforeReplay);
    console.log('C4A-12 PASS: exact start replay after delivery adds nothing');
    await change(id,{status:8},409); await change(s.cases.put,{status:8},409);
    assert.deepEqual(snapshot(id),beforeReplay);
    assert.equal(history(s.cases.put).length,4);
    console.log('C4A-13 PASS: rejected transitions add nothing');

    const race = await Promise.all([auth,startAuth].map(credential => fetch(`${url}/api/Cremations/${s.cases.race}/status`, {
      method:'PATCH', headers:{'Content-Type':'application/json',Authorization:`Bearer ${credential}`},
      body:JSON.stringify({status:5,notes:'Carrera C4A'}) })));
    assert.deepEqual(race.map(r=>r.status).sort(),[200,409]);
    assert.equal(history(s.cases.race).length,1); assert.equal(comments(s.cases.race).length,1);
    assert.equal(history(s.cases.race)[0].EstadoAnterior,4);
    assert.equal(history(s.cases.race)[0].EstadoNuevo,current(s.cases.race).Estado);
    assert.equal(history(s.cases.race)[0].ComentarioOperativoId,comments(s.cases.race)[0].Id);
    await Promise.all([1,2].map(() => update(s.cases.putRace,'2026-12-05T16:00:00Z')));
    assert.equal(history(s.cases.putRace).length,1);
    console.log('C4A-14 PASS: concurrent PATCH and PUT produce one committed transition each, no DB error');

    const beforeFailure = snapshot(s.cases.atomic);
    const beforePutFailure = snapshot(s.cases.putAtomic);
    sql(`CREATE FUNCTION c4a_fail() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'C4A injected failure'; END $$;
      CREATE TRIGGER c4a_fail BEFORE INSERT ON "${historyTable}" FOR EACH ROW EXECUTE FUNCTION c4a_fail();`);
    try {
      // Test-only database trigger forces a real late write failure, never production fault-injection code.
      const result = await fetch(`${url}/api/Cremations/${s.cases.atomic}/status`, {method:'PATCH',
        headers:{'Content-Type':'application/json',Authorization:`Bearer ${auth}`},
        body:JSON.stringify({status:6,notes:'Debe revertirse'})});
      assert.equal(result.status,500);
      const putResult = await fetch(`${url}/api/Cremations/${s.cases.putAtomic}`, {method:'PUT',
        headers:{'Content-Type':'application/json',Authorization:`Bearer ${auth}`},
        body:JSON.stringify({assignedToUserId:s.actor,cremationPackageId:s.package,scheduledAt:'2026-12-06T16:00:00Z'})});
      assert.equal(putResult.status,500);
      assert.deepEqual(snapshot(s.cases.atomic),beforeFailure);
      assert.deepEqual(snapshot(s.cases.putAtomic),beforePutFailure);
    } finally { sql(`DROP TRIGGER c4a_fail ON "${historyTable}"; DROP FUNCTION c4a_fail();`); }
    await change(s.cases.atomic,{status:6,notes:'Reintento correcto'});
    check(s.cases.atomic,1,5,6,s.other,'Reintento correcto','FechaFinalizacion');
    console.log('C4A-15 PASS: injected failure rolls back status/time/comment/history and PUT schedule');

    const inactiveBefore = snapshot(s.cases.inactive);
    await change(s.cases.inactive,{status:5},409,token(s.inactive));
    await change(s.cases.inactive,{status:5},409,token(randomUUID()));
    assert.deepEqual(snapshot(s.cases.inactive),inactiveBefore);
    await update(s.cases.putAtomic,'2026-12-06T16:00:00Z',{},409,token(s.inactive));
    assert.deepEqual(snapshot(s.cases.putAtomic),beforePutFailure);
    await change(s.cases.patchSchedule,{status:2,notes:'Programación por PATCH'});
    check(s.cases.patchSchedule,1,1,2,s.other,'Programación por PATCH');
    // Archive authority remains the existing policy; this separate fixture operation uses the actual Owner.
    sql(`UPDATE "Usuarios" SET "EsOwner"=true WHERE "Id"=${literal(s.other)};`);
    await api(s.cases.put,'DELETE',{requestId:randomUUID(),reason:'Archivo QA'},auth,204);
    const archivedHistory = history(s.cases.put);
    await api(`${s.cases.put}/status-transitions`,'GET',undefined,auth);
    await api(`${s.cases.put}/restore`,'PATCH',{requestId:randomUUID(),reason:'Restauración QA'},auth,204);
    assert.deepEqual(history(s.cases.put),archivedHistory);
    console.log('Additional PASS: inactive/missing actors rejected atomically; PATCH scheduling covered; archive/restore excluded');

    sql(`UPDATE "Usuarios" SET "Nombre"='Nombre posterior' WHERE "Id"=${literal(s.other)};`);
    const view = token(s.other,['Cremations.View']);
    const read = await api(`${id}/status-transitions`,'GET',undefined,view);
    assert.deepEqual(read.map(x=>x.sequence),[1,2,3,4,5,6]);
    assert.deepEqual(read.map(x=>[x.previousStatus,x.newStatus]),[[2,3],[3,4],[4,5],[5,6],[6,7],[7,8]]);
    assert.equal(read[1].actorUserId,s.other); assert.equal(read[1].actorNameSnapshot,'Operador QA C4A');
    assert.equal(read[0].operationalCommentId,comments(id).find(x=>x.Estado===3).Id);
    assert.equal(read[1].operationalCommentId,null);
    assert.deepEqual(Object.keys(read[0]).sort(),['id','cremationId','sequence','previousStatus','newStatus',
      'actorUserId','actorNameSnapshot','occurredAt','operationalCommentId'].sort());
    assert.deepEqual(await api(`${id}/status-transitions`,'GET',undefined,view),read);
    await api(`${id}/status-transitions`,'GET',undefined,null,401);
    await api(`${id}/status-transitions`,'GET',undefined,token(s.other,[]),403);
    await api(`${randomUUID()}/status-transitions`,'GET',undefined,view,404);
    await change(s.cases.inactive,{status:5},403,view);
    await api(`${id}/status-transitions`,'POST',{},auth,405);
    await api(`${id}/status-transitions`,'DELETE',{},auth,405);
    console.log('C4A-17 PASS: ordered read, stable snapshots, View allowed, forbidden/anonymous/missing blocked; no write endpoint');
    assert.deepEqual(snapshot(s.cases.legacy),legacy,'Unrelated transitions preserve all legacy evidence');
    await change(s.cases.legacy,{status:7});
    assert.equal(history(s.cases.legacy).length,1);
    assert.equal(history(s.cases.legacy)[0].EstadoAnterior,6);
    assert.equal(history(s.cases.legacy)[0].Secuencia,1);
    assert.deepEqual(comments(s.cases.legacy),legacy.comments);
    assert.deepEqual(evidence('VerificacionesInicioCremacion',s.cases.legacy),legacy.start);
    console.log('C4A-16 additional PASS: first future legacy transition starts sequence 1, no invented past');
    console.log('C4A ALL PASS');
  }
}
