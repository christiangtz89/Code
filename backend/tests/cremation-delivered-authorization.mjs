// Real-API regression; no packages required. Use ONLY a disposable PostgreSQL database.
// Apply the current migrations, start the API against that database, then set:
// C3F1_QA_CONTAINER=pcms-c3f1-..., C3F1_QA_DATABASE=pcms_c3f1_...,
// C3F1_QA_URL=http://127.0.0.1:<port>, C3F1_QA_STATE=<temporary JSON path>.
// Run `node backend/tests/cremation-delivered-authorization.mjs seed`, then `verify`.
// Before the fix, `red` must fail with HTTP 200 instead of 403 on both amendments.
// Dispose of the QA database/container and state file after testing.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C3F1_QA_CONTAINER;
const database = process.env.C3F1_QA_DATABASE;
const url = process.env.C3F1_QA_URL;
const statePath = process.env.C3F1_QA_STATE;
assert.match(container ?? '', /^pcms-c3f1-/);
assert.match(database ?? '', /^pcms_c3f1_/);
assert.match(url ?? '', /^http:\/\/127\.0\.0\.1:\d+$/);
assert.ok(statePath, 'Explicit temporary state path required');
const adminId = '11111111-1111-1111-1111-111111111111';
const password = 'Disposable-C3F1-password-42!';
const literal = value => value === null ? 'NULL' : `'${String(value).replaceAll("'", "''")}'`;
function sql(query) {
  return execFileSync('docker', ['exec', '-i', container, 'psql', '-X',
    '-v', 'ON_ERROR_STOP=1', '-U', 'postgres', '-d', database, '-At'],
  { input: query, encoding: 'utf8' }).trim();
}
const insert = (table, values) => sql(`INSERT INTO "${table}" (${Object.keys(values).map(x => `"${x}"`).join(',')}) VALUES (${Object.values(values).map(literal).join(',')});`);
const rows = table => JSON.parse(sql(`SELECT coalesce(json_agg(t ORDER BY t."Id"),'[]') FROM "${table}" t;`));
async function api(path, method = 'GET', body, token, expected = 200) {
  const response = await fetch(`${url}/api/${path}`, {
    method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  assert.equal(response.status, expected, `${method} ${path}: expected ${expected}, got ${response.status}: ${text.slice(0,300)}`);
  return text ? JSON.parse(text) : null;
}
const login = async actor => (await api('auth/login', 'POST', { email: actor.email, password })).token;
const historyTables = ['VerificacionesInicioCremacion', 'RevalidacionesPagoInicioCremacion',
  'ComentariosOperativosCremacion', 'ReasignacionesCremacion', 'EventosArchivoCremacion',
  'AclaracionesCremacion', 'CambiosInstruccionesCremacion', 'CambiosAccesorioCremacion'];
const snapshot = () => JSON.parse(sql(`SELECT json_build_object(${['Cremaciones', ...historyTables]
  .map(t => `${literal(t)}, (SELECT coalesce(json_agg(t ORDER BY t."Id"),'[]') FROM "${t}" t)`)
  .join(',')});`));
const surfaces = [
  { route: 'instructions-amendments', column: 'InstruccionesEspeciales', field: 'SpecialInstructions', table: 'CambiosInstruccionesCremacion', before: 'InstruccionesAnteriores', after: 'InstruccionesNuevas' },
  { route: 'accessory-amendments', column: 'DescripcionAccesorio', field: 'AccessoryDescription', table: 'CambiosAccesorioCremacion', before: 'DescripcionAnterior', after: 'DescripcionNueva' },
];

if (process.argv[2] === 'seed') {
  assert.equal(sql('SELECT count(*) FROM "Usuarios";'), '0', 'Fresh disposable database required');
  const s = { actors: {}, cases: {} };
  for (const name of ['owner', 'admin', 'fakeOwner', 'fakeAdmin', 'manager', 'ordinary']) {
    const email = `c3f1-${name}@example.invalid`;
    const registered = await api('auth/register', 'POST', { firstName: name, lastName: 'QA', email, password });
    s.actors[name] = { id: registered.userId, email, name: `${name} QA` };
  }
  // Trusted bootstrap of the ONLY real Owner; all subsequent role operations use supported APIs.
  sql(`UPDATE "Usuarios" SET "EsOwner"=true WHERE "Id"=${literal(s.actors.owner.id)};`);
  const ownerToken = await login(s.actors.owner);
  const permissions = await api('permissions', 'GET', undefined, ownerToken);
  const manageId = permissions.find(p => p.code === 'Cremations.Manage').id;
  for (const [actor, name] of [['fakeOwner', 'Owner'], ['fakeAdmin', 'Protected Admin'], ['manager', 'Manager'], ['ordinary', 'C3F1 Operator']]) {
    const role = await api('permissions/roles', 'POST', { name }, ownerToken);
    s.actors[actor].roleId = role.id;
    await api(`permissions/roles/${role.id}/permissions`, 'PUT', [manageId], ownerToken, 204);
    await api(`permissions/users/${s.actors[actor].id}/roles`, 'PUT', [role.id], ownerToken, 204);
  }
  await api(`permissions/users/${s.actors.admin.id}/roles`, 'PUT', [adminId], ownerToken, 204);
  await api('permissions/roles', 'POST', { name: 'Admin' }, ownerToken, 400);
  await api(`permissions/roles/${adminId}`, 'PUT', { name: 'Renamed Admin' }, ownerToken, 403);
  const now = new Date().toISOString();
  const customer = randomUUID();
  insert('Clientes', { Id: customer, Nombre: 'Cliente', ApellidoPaterno: 'QA', Telefono: '000',
    CorreoElectronico: 'c3f1-client@example.invalid', Activo: true, FechaCreacion: now });
  for (const [name, status] of [['red',8], ['delivered',8], ['inProgress',3], ['cooling',4], ['processing',5], ['completed',6], ['ready',7], ['cancelled',9]]) {
    const pet = randomUUID(); const reception = randomUUID(); const id = randomUUID();
    s.cases[name] = id;
    insert('Mascotas', { Id: pet, CustomerId: customer, Nombre: `QA ${name}`, Especie: 'Canino',
      Raza: 'QA', Sexo: 'M', Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-10-01', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: s.actors.owner.id,
      FechaRecepcion: now, CodigoQr: `C3F1-${name}`, PesoVerificadoKg: 5,
      TieneObjetosPersonales: false, Activo: true, FechaCreacion: now,
      NombreClienteSnapshot: 'Cliente QA', NombreMascotaSnapshot: `QA ${name}` });
    insert('Cremaciones', { Id: id, RecepcionId: reception, AsignadoAUsuarioId: s.actors.owner.id,
      TipoCremacion: 1, Estado: status, NombrePaquete: 'C3F1 QA', IncluyeUrna: false,
      IncluyeHuella: true, IncluyeCertificado: false, FechaInicio: now,
      InstruccionesEspeciales: 'Instrucciones originales', DescripcionAccesorio: 'Accesorio original',
      Notas: 'Nota general original', PrecioCotizado: 300, MontoPagoRequeridoInicio: 0,
      PesoCotizadoKg: 5, PesoMinimoCotizadoKg: 0, PesoMaximoCotizadoKg: 5, Activo: true, FechaCreacion: now });
    insert('VerificacionesInicioCremacion', { Id: randomUUID(), CremacionId: id, RecepcionId: reception,
      CodigoQrRecepcionSnapshot: `C3F1-${name}`, ConfirmadoPorUsuarioId: s.actors.owner.id,
      NombreUsuarioConfirmoSnapshot: s.actors.owner.name, FechaConfirmacion: now, SolicitudId: randomUUID() });
    insert('ComentariosOperativosCremacion', { Id: randomUUID(), CremacionId: id, Estado: 3,
      Comentario: 'Evidencia previa de inicio', CreadoPorUsuarioId: s.actors.owner.id,
      NombreUsuarioSnapshot: s.actors.owner.name, FechaCreacion: now });
  }
  writeFileSync(statePath, JSON.stringify(s, null, 2), { mode: 0o600 });
  console.log('Seed PASS: disposable actors, supported role-management APIs, protected Admin name constraints, C1A/C3 fixtures');
} else {
  const s = JSON.parse(readFileSync(statePath, 'utf8'));
  const tokens = {};
  for (const [name, actor] of Object.entries(s.actors)) tokens[name] = await login(actor);
  const checkIdentityClaims = () => {
    for (const [name, token] of Object.entries(tokens)) {
      const claims = JSON.parse(Buffer.from(token.split('.')[1], 'base64url').toString());
      assert.equal(claims.pcms_owner === 'true', name === 'owner', `${name}: actual Owner claim`);
      assert.equal(claims.pcms_protected_admin === 'true', name === 'admin', `${name}: protected Admin claim`);
    }
  };
  if (process.argv[2] === 'claims') {
    checkIdentityClaims();
    console.log('PASS authoritative identity claims');
    process.exit(0);
  }
  const current = id => rows('Cremaciones').find(x => x.Id === id);
  const payload = (id, surface) => ({ requestId: randomUUID(), reason: 'Corrección QA con motivo',
    [`new${surface.field}`]: `Valor corregido ${randomUUID()}`,
    [`expectedCurrent${surface.field}`]: current(id)[surface.column] });
  async function reject(actor, id, surface, extras = {}) {
    const before = snapshot();
    await api(`cremations/${id}/${surface.route}`, 'POST', { ...payload(id, surface), ...extras }, tokens[actor], 403);
    assert.deepEqual(snapshot(), before, 'Rejected request must preserve every current row and all C1A/C3 history');
  }
  async function accept(actor, id, surface, role) {
    const before = snapshot(); const body = payload(id, surface);
    await api(`cremations/${id}/${surface.route}`, 'POST', body, tokens[actor]);
    const after = snapshot();
    const newRows = after[surface.table].filter(x => !before[surface.table].some(y => y.Id === x.Id));
    assert.equal(newRows.length, 1, 'Exactly one new amendment');
    const evidence = newRows[0];
    assert.equal(evidence.CremacionId, id);
    assert.equal(evidence.RequestId, body.requestId);
    assert.equal(evidence.Secuencia, before[surface.table].filter(x => x.CremacionId === id).length + 1);
    assert.equal(evidence[surface.before], body[`expectedCurrent${surface.field}`]);
    assert.equal(evidence[surface.after], body[`new${surface.field}`]);
    assert.equal(evidence.ActorUsuarioId, s.actors[actor].id);
    assert.equal(evidence.NombreActorSnapshot, s.actors[actor].name);
    assert.equal(evidence.RolActor, role);
    assert.equal(evidence.Motivo, body.reason);
    assert.equal(evidence.Estado, current(id).Estado);
    assert.ok(Number.isFinite(Date.parse(evidence.FechaCreacion)));
    const expected = structuredClone(before);
    expected.Cremaciones.find(x => x.Id === id)[surface.column] = body[`new${surface.field}`];
    expected[surface.table].push(evidence);
    expected[surface.table].sort((a,b) => a.Id.localeCompare(b.Id));
    assert.deepEqual(after, expected, 'Only the requested value and one amendment may change');
    await api(`cremations/${id}/${surface.route}`, 'POST', body, tokens[actor]);
    assert.deepEqual(snapshot(), after, 'Exact retry must not alter immutable evidence');
  }
  async function checkSplitRolePermissions() {
    const permissions = await api('permissions', 'GET', undefined, tokens.owner);
    const roles = await api('permissions/roles', 'GET', undefined, tokens.owner);
    const codes = roles.find(x => x.id === adminId).permissions;
    const original = permissions.filter(x => codes.includes(x.code)).map(x => x.id);
    const withoutManage = permissions.filter(x => codes.includes(x.code) && x.code !== 'Cremations.Manage').map(x => x.id);
    try {
      await api(`permissions/users/${s.actors.admin.id}/roles`, 'PUT', [adminId, s.actors.ordinary.roleId], tokens.owner, 204);
      await api(`permissions/roles/${adminId}/permissions`, 'PUT', withoutManage, tokens.owner, 204);
      tokens.admin = await login(s.actors.admin);
      const claims = JSON.parse(Buffer.from(tokens.admin.split('.')[1], 'base64url').toString());
      assert.ok([claims.permission].flat().includes('Cremations.Manage'), 'Ordinary role still grants Manage');
      assert.notEqual(claims.pcms_protected_admin, 'true', 'Delivered visibility must require Manage on the protected role itself');
      for (const surface of surfaces) await reject('admin', s.cases.delivered, surface);
      console.log('PASS split-role permissions: inherited Manage cannot enable protected Delivered actions');
    } finally {
      await api(`permissions/roles/${adminId}/permissions`, 'PUT', original, tokens.owner, 204);
      await api(`permissions/users/${s.actors.admin.id}/roles`, 'PUT', [adminId], tokens.owner, 204);
      tokens.admin = await login(s.actors.admin);
    }
  }
  if (process.argv[2] === 'split') {
    await checkSplitRolePermissions();
    process.exit(0);
  }
  if (process.argv[2] === 'red') {
    let failures = 0;
    for (const surface of surfaces) {
      try { await reject('fakeOwner', s.cases.red, surface); }
      catch (error) { failures++; console.error(`RED ${surface.route}: ${error.message}`); }
    }
    assert.equal(failures, 0, 'Custom Owner role must not authorize either Delivered correction');
  } else {
    assert.equal(process.argv[2], 'verify');
    checkIdentityClaims();
    await checkSplitRolePermissions();
    for (const actor of ['fakeOwner','fakeAdmin','manager','ordinary']) {
      for (const surface of surfaces) await reject(actor, s.cases.delivered, surface);
      console.log(`PASS Delivered rejection: ${actor}; no mutation or history`);
    }
    // Direct requests may include invented identity fields: the API must ignore them.
    for (const surface of surfaces) await reject('fakeOwner', s.cases.delivered, surface,
      { actorUserId: s.actors.owner.id, actorRole: 'Admin', isOwner: true, privileged: true });
    console.log('PASS direct API spoofed actor/role/privilege fields');
    for (const [actor, role] of [['owner','Owner'],['admin','Admin']]) {
      for (const surface of surfaces) await accept(actor, s.cases.delivered, surface, role);
      console.log(`PASS Delivered ${actor}: both amendments, complete evidence, exact retry`);
    }
    for (const state of ['inProgress','cooling','processing','completed','ready']) {
      for (const [actor, role] of [['manager','Manager'],['ordinary','C3F1 Operator'],['fakeOwner','Owner']]) {
        for (const surface of surfaces) await accept(actor, s.cases[state], surface, role);
      }
      console.log(`PASS active regression: ${state}; Manager/custom roles retain existing access`);
    }
    for (const actor of ['fakeOwner','fakeAdmin','manager','ordinary']) {
      for (const surface of surfaces) await reject(actor, s.cases.cancelled, surface);
    }
    for (const [actor, role] of [['owner','Owner'],['admin','Admin']]) {
      for (const surface of surfaces) await accept(actor, s.cases.cancelled, surface, role);
    }
    console.log('PASS Cancelled regression and C1A/C3 historical preservation throughout');
    // Preserve a valid login while revoking authoritative privilege in the database.
    await api(`permissions/users/${s.actors.admin.id}/roles`, 'PUT', [s.actors.ordinary.roleId], tokens.owner, 204);
    for (const surface of surfaces) await reject('admin', s.cases.delivered, surface);
    await api(`permissions/users/${s.actors.admin.id}/roles`, 'PUT', [adminId], tokens.owner, 204);
    sql(`UPDATE "Usuarios" SET "Activo"=false WHERE "Id"=${literal(s.actors.owner.id)};`);
    for (const surface of surfaces) await reject('owner', s.cases.delivered, surface);
    sql(`UPDATE "Usuarios" SET "Activo"=true WHERE "Id"=${literal(s.actors.owner.id)};`);
    console.log('PASS stale Admin token and inactive actual Owner rejected');
    // Defense-in-depth: an inconsistent imported role with Name=Admin is still NOT protected.
    // Supported APIs prohibit this state; mutate ONLY this disposable fixture and restore it.
    try {
      sql(`UPDATE "Roles" SET "Name"='Reserved QA Admin', "NormalizedName"='RESERVED QA ADMIN' WHERE "Id"=${literal(adminId)};
        UPDATE "Roles" SET "Name"='Admin', "NormalizedName"='ADMIN' WHERE "Id"=${literal(s.actors.fakeAdmin.roleId)};`);
      tokens.fakeAdmin = await login(s.actors.fakeAdmin);
      assert.notEqual(JSON.parse(Buffer.from(tokens.fakeAdmin.split('.')[1], 'base64url')).pcms_protected_admin, 'true');
      for (const surface of surfaces) await reject('fakeAdmin', s.cases.delivered, surface);
      console.log('PASS exact custom Admin name at a non-protected role ID');
    } finally {
      sql(`UPDATE "Roles" SET "Name"='Protected Admin', "NormalizedName"='PROTECTED ADMIN' WHERE "Id"=${literal(s.actors.fakeAdmin.roleId)};
        UPDATE "Roles" SET "Name"='Admin', "NormalizedName"='ADMIN' WHERE "Id"=${literal(adminId)};`);
    }
    console.log('F1-1 through F1-9 PASS');
  }
}
