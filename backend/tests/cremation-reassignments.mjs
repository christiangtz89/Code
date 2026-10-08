// Real API/SQL regression. Disposable pcms_c3b1_* database only; no dependencies.
// Set C3B1_QA_CONTAINER, DATABASE, URL, KEY, STATE (each with C3B1_QA_ prefix).
// Apply migrations, run seed, then verify. Baseline-only red checks ordinary PUT.
// Dispose of the entire QA container and temporary state afterwards.
import assert from 'node:assert/strict';
import { createHmac, randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C3B1_QA_CONTAINER;
const database = process.env.C3B1_QA_DATABASE;
const url = process.env.C3B1_QA_URL;
const key = process.env.C3B1_QA_KEY;
const statePath = process.env.C3B1_QA_STATE;
assert.match(container ?? '', /^pcms-c3b1-/);
assert.match(database ?? '', /^pcms_c3b1_/);
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
const token = (actor, manage = true, claims = {}) => {
  const encode = object => Buffer.from(JSON.stringify(object)).toString('base64url');
  const data = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({
    sub: actor, iss: 'c3b1-qa', aud: 'c3b1-qa', exp: Math.floor(Date.now() / 1000) + 3600,
    permission: manage ? ['Cremations.View', 'Cremations.Manage'] : ['Cremations.View'], ...claims,
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
    CorreoElectronico: 'c3b1@example.invalid', HashContrasena: 'NOT_A_PASSWORD', Activo: true, EsOwner: false, FechaCreacion: now });
  insert('Clientes', { Id: state.customer, Nombre: 'Cliente', ApellidoPaterno: 'QA',
    Telefono: '000', CorreoElectronico: 'client@example.invalid', Activo: true, FechaCreacion: now });
  insert('PaquetesCremacion', { Id: state.package, Nombre: 'C3B1 QA', TipoPaquete: 1,
    IncluyeUrna: false, IncluyeHuella: false, IncluyeCertificado: false,
    EsPublico: false, OrdenVisualizacion: 0, Activo: true, FechaCreacion: now });
  for (const [kind, status] of [['red', 4], ['main', 2], ['legacy', 8], ['cancelled', 1]]) {
    const pet = randomUUID(); const reception = randomUUID();
    insert('Mascotas', { Id: pet, CustomerId: state.customer, Nombre: `QA ${kind}`, Especie: 'Canino',
      Raza: 'QA', Sexo: 'M', Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-10-01', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: state.actor,
      FechaRecepcion: '2026-10-01T00:00:00Z', CodigoQr: `C3B1-${kind.toUpperCase()}`,
      PesoVerificadoKg: 5, TieneObjetosPersonales: false, Activo: true, FechaCreacion: now,
      NombreClienteSnapshot: 'Cliente QA', NombreMascotaSnapshot: `QA ${kind}` });
    insert('Cremaciones', { Id: state[kind], RecepcionId: reception, AsignadoAUsuarioId: state.actor,
      TipoCremacion: 1, Estado: status, NombrePaquete: 'C3B1 QA', PaqueteCremacionId: state.package,
      IncluyeUrna: false, IncluyeHuella: false, IncluyeCertificado: false,
      FechaProgramada: kind === 'main' ? state.scheduledAt : null,
      Notas: kind === 'legacy' ? 'Texto legado: procedencia desconocida' : 'Nota general original',
      PrecioCotizado: 300, MontoPagoRequeridoInicio: 0, PesoCotizadoKg: 5,
      PesoMinimoCotizadoKg: 0, PesoMaximoCotizadoKg: 5, Activo: true, FechaCreacion: now });
  }
  state.target = randomUUID(); state.other = randomUUID(); state.inactive = randomUUID();
  state.manager = randomUUID(); state.owner = randomUUID(); state.admin = randomUUID();
  state.managerRole = randomUUID();
  for (const kind of ['target', 'other', 'inactive', 'manager', 'owner', 'admin']) {
    insert('Usuarios', { Id: state[kind], Nombre: kind, Apellido: 'QA',
      CorreoElectronico: `${kind}@example.invalid`, HashContrasena: 'NOT_A_PASSWORD',
      Activo: kind !== 'inactive', EsOwner: kind === 'owner', FechaCreacion: now });
  }
  insert('Roles', { Id: state.managerRole, Name: 'Manager', NormalizedName: 'MANAGER', IsActive: true });
  insert('UsuarioRoles', { UserId: state.manager, RoleId: state.managerRole });
  insert('UsuarioRoles', { UserId: state.admin, RoleId: '11111111-1111-1111-1111-111111111111' });
  sql(`INSERT INTO "RolPermisos" ("RoleId","PermissionId") SELECT ${literal(state.managerRole)},"Id" FROM "Permisos" WHERE "Code"='Cremations.Manage';`);
  state.legacyBefore = rows('Cremaciones', `"Id"=${literal(state.legacy)}`)[0];
  writeFileSync(statePath, JSON.stringify(state, null, 2));
  console.log('Seeded isolated fixtures, including ambiguous legacy Notes.');
} else {
  const s = JSON.parse(readFileSync(statePath, 'utf8'));
  const current = id => rows('Cremaciones', `"Id"=${literal(id)}`)[0];
  const history = id => rows('ReasignacionesCremacion', `"CremacionId"=${literal(id)}`).sort((a,b) => a.Secuencia-b.Secuencia);
  const update = (id, assigned, expected=200) => api(id, 'PUT', {
    assignedToUserId: assigned, cremationPackageId: s.package, urnId: null,
    scheduledAt: current(id).FechaProgramada ? new Date(current(id).FechaProgramada).toISOString() : null, notes: current(id).Notas,
  }, token(s.actor), expected);
  const change = (status, extra={}) => api(`${s.main}/status`, 'PATCH', {status, ...extra}, token(s.actor));
  const reassign = (id, actor, target, expected=200, extra={}) => api(`${id}/reassignments`, 'POST', {
    requestId: randomUUID(), newAssignedToUserId: target, reason: 'Cambio de turno', ...extra,
  }, token(actor), expected);
  if (process.argv[2] === 'red') {
    await update(s.red, s.target, 409);
    console.log('RED unexpectedly passed');
    process.exit(0);
  }
  assert.equal(process.argv[2], 'verify');
  assert.equal(sql('SELECT count(*) FROM "ReasignacionesCremacion";'), '0');
  assert.deepEqual(current(s.legacy), s.legacyBefore);
  assert.equal((await api(s.legacy, 'GET', undefined, token(s.actor))).status, 8);
  assert.deepEqual(await api(`${s.legacy}/reassignments`, 'GET', undefined, token(s.actor, false)), []);
  console.log('B1-12 PASS: legacy readable, unchanged, no invented history');
  await update(s.cancelled, s.target);
  assert.equal(current(s.cancelled).AsignadoAUsuarioId, s.target);
  assert.deepEqual(history(s.cancelled), []);
  console.log('B1-1 PASS: pre-start ordinary assignment remains supported');
  await change(3, {requestId: s.request, receptionQrCode: 'C3B1-MAIN', custodyAccepted: true, notes:'Inicio original'});
  const verification = rows('VerificacionesInicioCremacion', `"CremacionId"=${literal(s.main)}`);
  assert.equal(verification.length, 1);
  const original = current(s.main);
  await update(s.main, s.target, 409);
  assert.deepEqual(current(s.main), original); assert.deepEqual(history(s.main), []);
  console.log('B1-2 PASS: ordinary replacement rejected without side effects');
  await update(s.main, null, 409);
  assert.deepEqual(current(s.main), original); assert.deepEqual(history(s.main), []);
  console.log('B1-3 PASS: ordinary clearing rejected');
  await update(s.main, s.actor);
  await reassign(s.main, s.manager, s.target, 400, {reason:'   '});
  await reassign(s.main, s.manager, s.inactive, 400);
  assert.deepEqual(history(s.main), []);
  console.log('B1-9 PASS: inactive employee rejected');
  await reassign(s.main, s.actor, s.target, 403);
  await reassign(s.main, s.inactive, s.target, 403);
  await api(`${s.main}/reassignments`, 'POST', {requestId:randomUUID(),newAssignedToUserId:s.target,reason:'Rol declarado'}, token(s.actor, true, {role:'Admin',pcms_owner:'true'}), 403);
  await api(`${s.main}/reassignments`, 'POST', {requestId:randomUUID(),newAssignedToUserId:s.target,reason:'No autorizado'}, token(s.manager, false), 403);
  await api(`${s.main}/reassignments`, 'POST', {}, undefined, 401);
  assert.deepEqual(history(s.main), []);
  console.log('B1-8 PASS: ordinary Manage, view-only and anonymous rejected');
  const requestId = randomUUID();
  await reassign(s.main, s.manager, s.target, 200, {requestId});
  const first = history(s.main);
  assert.equal(first.length, 1);
  assert.equal(current(s.main).AsignadoAUsuarioId, s.target);
  assert.equal(first[0].AnteriorUsuarioId, s.actor);
  assert.equal(first[0].NuevoUsuarioId, s.target);
  assert.equal(first[0].ActorUsuarioId, s.manager);
  assert.equal(first[0].RolActor, 'Manager');
  assert.equal(first[0].Motivo, 'Cambio de turno');
  assert.equal(first[0].Estado, 3);
  assert.equal(first[0].Secuencia, 1);
  assert.equal(first[0].NombreAnteriorSnapshot, s.actorName);
  assert.equal(first[0].NombreNuevoSnapshot, 'target QA');
  assert.equal(first[0].NombreActorSnapshot, 'manager QA');
  assert.ok(Number.isFinite(Date.parse(first[0].FechaCreacion)));
  await reassign(s.main, s.manager, s.target, 200, {requestId});
  assert.deepEqual(history(s.main), first);
  await reassign(s.main, s.manager, s.other, 409, {requestId});
  console.log('B1-4 PASS: Manager controlled amendment and exact retry evidence');
  assert.deepEqual(rows('VerificacionesInicioCremacion', `"CremacionId"=${literal(s.main)}`), verification);
  console.log('B1-10 PASS: entire original C1A verification unchanged');
  await Promise.all([
    reassign(s.main, s.manager, s.other),
    reassign(s.main, s.manager, s.actor),
  ]);
  const concurrent = history(s.main);
  assert.equal(concurrent.length, 3);
  for (let i=1; i<concurrent.length; i++) {
    assert.equal(concurrent[i].AnteriorUsuarioId, concurrent[i-1].NuevoUsuarioId);
    assert.equal(concurrent[i].Secuencia, i+1);
  }
  assert.equal(current(s.main).AsignadoAUsuarioId, concurrent.at(-1).NuevoUsuarioId);
  console.log('B1-11 PASS: concurrent updates serialize into coherent immutable chain');
  await change(4); await change(5); await change(6);
  await reassign(s.main, s.manager, s.target);
  await change(7);
  await reassign(s.main, s.manager, s.other);
  assert.deepEqual(history(s.main).slice(-2).map(x=>x.Estado), [6,7]);
  console.log('B1-5 PASS: Completed and ReadyForDelivery controlled reassignment');
  await change(8);
  const delivered = current(s.main); const before = history(s.main);
  await reassign(s.main, s.manager, s.target, 403);
  await update(s.main, s.target, 409);
  assert.deepEqual(current(s.main), delivered); assert.deepEqual(history(s.main), before);
  console.log('B1-6 PASS: Delivered Manager rejected without mutation');
  await reassign(s.main, s.owner, s.target);
  await reassign(s.main, s.admin, s.actor);
  assert.deepEqual(history(s.main).slice(-2).map(x=>x.RolActor), ['Owner','Admin']);
  assert.deepEqual(rows('VerificacionesInicioCremacion', `"CremacionId"=${literal(s.main)}`), verification);
  assert.deepEqual(history(s.main).slice(0,before.length), before);
  console.log('B1-7 PASS: Delivered Owner/protected Admin corrections; prior evidence preserved');
  const read = await api(`${s.main}/reassignments`, 'GET', undefined, token(s.actor, false));
  assert.equal(read.length, history(s.main).length);
  console.log('All B1 API/SQL checks passed');
}
