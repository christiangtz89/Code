// Integration regression against a disposable PostgreSQL database and the real API.
// No packages required. Never point this at development/production data.
// 1. Migrate an isolated pcms_c3a_* database to the pre-C3-A checkpoint.
// 2. Set C3A_QA_CONTAINER, C3A_QA_DATABASE, C3A_QA_URL, C3A_QA_KEY,
//    C3A_QA_STATE (a temporary JSON path), and run this file with `seed`.
// 3. Before implementation, `red` must fail on general-note preservation.
// 4. Apply the new migration, restart the API, then run with `verify`.
// Later regression runs may use the full current chain, then `seed` and `verify`.
// Dispose of the entire QA database/container afterwards.
import assert from 'node:assert/strict';
import { createHmac, randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C3A_QA_CONTAINER;
const database = process.env.C3A_QA_DATABASE;
const url = process.env.C3A_QA_URL;
const key = process.env.C3A_QA_KEY;
const statePath = process.env.C3A_QA_STATE;
assert.match(container ?? '', /^pcms-c3a-/);
assert.match(database ?? '', /^pcms_c3a_/);
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
const token = (actor, manage = true) => {
  const encode = object => Buffer.from(JSON.stringify(object)).toString('base64url');
  const data = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({
    sub: actor, iss: 'c3a-qa', aud: 'c3a-qa', exp: Math.floor(Date.now() / 1000) + 3600,
    permission: manage ? ['Cremations.View', 'Cremations.Manage'] : ['Cremations.View'],
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

if (process.argv[2] === 'seed') {
  assert.equal(sql('SELECT count(*) FROM "Cremaciones";'), '0', 'Use a fresh disposable database');
  const state = { actor: randomUUID(), customer: randomUUID(), package: randomUUID(),
    red: randomUUID(), main: randomUUID(), legacy: randomUUID(), cancelled: randomUUID(),
    request: randomUUID(), actorName: 'Operador QA Apellido QA',
    scheduledAt: new Date(Math.floor(Date.now() / 3600000) * 3600000).toISOString() };
  const now = new Date().toISOString();
  insert('Usuarios', { Id: state.actor, Nombre: 'Operador QA', Apellido: 'Apellido QA',
    CorreoElectronico: 'c3a@example.invalid', HashContrasena: 'NOT_A_PASSWORD', Activo: true, EsOwner: false, FechaCreacion: now });
  insert('Clientes', { Id: state.customer, Nombre: 'Cliente', ApellidoPaterno: 'QA',
    Telefono: '000', CorreoElectronico: 'client@example.invalid', Activo: true, FechaCreacion: now });
  insert('PaquetesCremacion', { Id: state.package, Nombre: 'C3A QA', TipoPaquete: 1,
    IncluyeUrna: false, IncluyeHuella: false, IncluyeCertificado: false,
    EsPublico: false, OrdenVisualizacion: 0, Activo: true, FechaCreacion: now });
  for (const [kind, status] of [['red', 3], ['main', 2], ['legacy', 8], ['cancelled', 1]]) {
    const pet = randomUUID(); const reception = randomUUID();
    insert('Mascotas', { Id: pet, CustomerId: state.customer, Nombre: `QA ${kind}`, Especie: 'Canino',
      Raza: 'QA', Sexo: 'M', Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-10-01', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: state.actor,
      FechaRecepcion: '2026-10-01T00:00:00Z', CodigoQr: `C3A-${kind.toUpperCase()}`,
      PesoVerificadoKg: 5, TieneObjetosPersonales: false, Activo: true, FechaCreacion: now,
      NombreClienteSnapshot: 'Cliente QA', NombreMascotaSnapshot: `QA ${kind}` });
    insert('Cremaciones', { Id: state[kind], RecepcionId: reception, AsignadoAUsuarioId: state.actor,
      TipoCremacion: 1, Estado: status, NombrePaquete: 'C3A QA', PaqueteCremacionId: state.package,
      IncluyeUrna: false, IncluyeHuella: false, IncluyeCertificado: false,
      FechaProgramada: kind === 'main' ? state.scheduledAt : null,
      Notas: kind === 'legacy' ? 'Texto legado: procedencia desconocida' : 'Nota general original',
      PrecioCotizado: 300, MontoPagoRequeridoInicio: 0, PesoCotizadoKg: 5,
      PesoMinimoCotizadoKg: 0, PesoMaximoCotizadoKg: 5, Activo: true, FechaCreacion: now });
  }
  state.legacyBefore = rows('Cremaciones', `"Id"=${literal(state.legacy)}`)[0];
  writeFileSync(statePath, JSON.stringify(state, null, 2));
  console.log('Seeded isolated fixtures, including ambiguous legacy Notes.');
} else {
  const s = JSON.parse(readFileSync(statePath, 'utf8'));
  const auth = token(s.actor);
  const current = id => rows('Cremaciones', `"Id"=${literal(id)}`)[0];
  const comments = id => rows('ComentariosOperativosCremacion', `"CremacionId"=${literal(id)}`);
  const change = (id, body, expected = 200, credential = auth) => api(`${id}/status`, 'PATCH', body, credential, expected);
  const update = (notes, extra = {}, credential = auth, expected = 200) => api(s.main, 'PUT', {
    assignedToUserId: s.actor, cremationPackageId: s.package, urnId: null,
    accessoryDescription: null, scheduledAt: s.scheduledAt, specialInstructions: null, notes, ...extra,
  }, credential, expected);

  if (process.argv[2] === 'red') {
    await change(s.red, { status: 4, notes: 'Comentario operativo RED' });
    assert.equal(current(s.red).Notas, 'Nota general original', 'Status commentary must not replace general notes');
    console.log('RED probe unexpectedly passed');
  } else {
    assert.equal(process.argv[2], 'verify');
    assert.equal(sql('SELECT count(*) FROM "ComentariosOperativosCremacion";'), '0', 'Migration must not fabricate commentary');
    assert.deepEqual(current(s.legacy), s.legacyBefore);
    const legacy = await api(s.legacy, 'GET', undefined, auth);
    assert.equal(legacy.notes, 'Texto legado: procedencia desconocida');
    assert.deepEqual(await api(`${s.legacy}/operational-comments`, 'GET', undefined, auth), []);
    console.log('C3A-8 PASS: legacy row unchanged; readable text, no invented evidence');

    // A fresh regression run need not execute the baseline-only RED probe.
    // Release its fixture's horno without inventing a comment.
    if (current(s.red).Estado === 3) {
      await change(s.red, { status: 4 });
      assert.deepEqual(comments(s.red), []);
    }
    const start = { status: 3, requestId: s.request, receptionQrCode: 'C3A-MAIN', custodyAccepted: true, notes: 'Comentario de inicio' };
    const rejectedBefore = current(s.main);
    await change(s.main, { ...start, receptionQrCode: 'C3A-LEGACY' }, 409);
    assert.deepEqual(current(s.main), rejectedBefore);
    assert.deepEqual(comments(s.main), []);
    const started = await change(s.main, start);
    assert.equal(started.status, 3);
    assert.equal(started.notes, 'Nota general original');
    assert.equal(current(s.main).Notas, 'Nota general original');
    const first = comments(s.main);
    assert.equal(first.length, 1);
    assert.equal(first[0].Estado, 3);
    assert.equal(first[0].Comentario, 'Comentario de inicio');
    assert.equal(first[0].CreadoPorUsuarioId, s.actor);
    assert.equal(first[0].NombreUsuarioSnapshot, s.actorName);
    assert.equal(first[0].FechaCreacion, current(s.main).FechaInicio);
    console.log('C3A-1 PASS: atomic start commentary independent of general notes; actor/status/time verified');

    await update('Nota general editada', { operationalComments: [{ ...first[0], Comentario: 'FORGED', Estado: 9,
      CreadoPorUsuarioId: randomUUID(), FechaCreacion: '2000-01-01T00:00:00Z' }] });
    assert.equal(current(s.main).Notas, 'Nota general editada');
    assert.deepEqual(comments(s.main), first);
    console.log('C3A-2 PASS: general-note replacement cannot alter commentary/context/actor/time');
    await update(null, { operationalComments: [] });
    assert.equal(current(s.main).Notas, null);
    assert.deepEqual(comments(s.main), first);
    console.log('C3A-3 PASS: clearing general notes preserves commentary');

    await change(s.main, { status: 4, notes: 'Comentario de enfriamiento' });
    await change(s.main, { status: 5, notes: 'Comentario de procesamiento' });
    const three = comments(s.main);
    assert.equal(three.length, 3);
    assert.deepEqual(three.find(c => c.Estado === 3), first[0]);
    assert.equal(three.find(c => c.Estado === 4).Comentario, 'Comentario de enfriamiento');
    assert.equal(three.find(c => c.Estado === 5).Comentario, 'Comentario de procesamiento');
    assert.equal(current(s.main).Notas, null);
    for (const comment of three) {
      assert.equal(comment.CreadoPorUsuarioId, s.actor);
      assert.equal(comment.NombreUsuarioSnapshot, s.actorName);
      assert.ok(Number.isFinite(Date.parse(comment.FechaCreacion)));
    }
    console.log('C3A-4 PASS: Cooling and ProcessingRemains append independent commentary');

    // Empty, absent and whitespace-only comments are all intentionally absent evidence.
    await change(s.main, { status: 6 });
    await change(s.main, { status: 7, notes: '   ' });
    await change(s.main, { status: 8, notes: '' });
    assert.deepEqual(comments(s.main), three);
    console.log('C3A-5 PASS: absent/whitespace/empty comments create no rows');
    const progressed = current(s.main);
    const replay = await change(s.main, start);
    assert.equal(replay.status, 8);
    assert.deepEqual(current(s.main), progressed);
    assert.deepEqual(comments(s.main), three);
    assert.equal(rows('VerificacionesInicioCremacion', `"CremacionId"=${literal(s.main)}`).length, 1);
    console.log('C3A-6 PASS: exact start replay after progression preserves one original comment and current status');

    await update('UNAUTHORIZED', {}, token(s.actor, false), 403);
    await change(s.cancelled, { status: 9, notes: 'UNAUTHORIZED' }, 403, token(s.actor, false));
    await change(s.cancelled, { status: 9, notes: 'ANONYMOUS' }, 401, null);
    assert.deepEqual(current(s.main), progressed);
    assert.deepEqual(comments(s.main), three);
    assert.equal(current(s.cancelled).Estado, 1);
    assert.deepEqual(comments(s.cancelled), []);
    console.log('C3A-7 PASS: unauthorized callers cannot change either note surface');

    await change(s.cancelled, { status: 9, notes: 'Comentario de cancelación' });
    assert.equal(comments(s.cancelled)[0].Estado, 9);
    assert.equal(current(s.cancelled).Notas, 'Nota general original');
    sql(`UPDATE "Usuarios" SET "Nombre"='Nombre posterior' WHERE "Id"=${literal(s.actor)};`);
    const read = await api(`${s.main}/operational-comments`, 'GET', undefined, token(s.actor, false));
    assert.equal(read.length, 3);
    assert.deepEqual(read.map(c => c.status), [3, 4, 5]);
    assert.equal(read[0].comment, 'Comentario de inicio');
    assert.equal(read[0].cremationId, s.main);
    assert.equal(read[0].createdByUserId, s.actor);
    assert.equal(read[0].createdByUserNameSnapshot, s.actorName);
    assert.equal(Date.parse(read[0].createdAt), Date.parse(first[0].FechaCreacion));
    await api(`${s.main}/operational-comments`, 'GET', undefined, null, 401);
    await api(`${randomUUID()}/operational-comments`, 'GET', undefined, auth, 404);
    console.log('Additional PASS: cancellation, actor-name snapshot, authorized read, missing-case behavior');
  }
}
