// Real-login/API regression for C6-A. ONLY an isolated, disposable PostgreSQL database.
// Required: C6A_QA_CONTAINER=pcms-c6a-*, C6A_QA_DATABASE=pcms_c6a_*,
// C6A_QA_URL=http://127.0.0.1:<port>, C6A_QA_STATE=/tmp/<state>.json.
// Migrate/start the API, then run `node <this-file> seed` and `verify`.
// `expiry` additionally needs C6A_QA_EXPIRED_URL: same key/issuer/audience, expiry=-10.
// `failure` renames ONLY this disposable database's role-permission table, restoring in finally.
// Removing the current-DB gate must fail inactive/revoked/granted authority assertions.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C6A_QA_CONTAINER;
const database = process.env.C6A_QA_DATABASE;
const url = process.env.C6A_QA_URL;
const statePath = process.env.C6A_QA_STATE;
assert.match(container ?? '', /^pcms-c6a-/);
assert.match(database ?? '', /^pcms_c6a_/);
assert.match(url ?? '', /^http:\/\/127\.0\.0\.1:\d+$/);
assert.match(statePath ?? '', /^\/tmp\/.+\.json$/);
const password = 'C6A-disposable-only-42!';
const adminId = '11111111-1111-1111-1111-111111111111';
const lit = value => value === null ? 'NULL' : `'${String(value).replaceAll("'", "''")}'`;
const sql = query => execFileSync('docker', ['exec', '-i', container, 'psql', '-X',
  '-v', 'ON_ERROR_STOP=1', '-U', 'postgres', '-d', database, '-At'],
{ input: query, encoding: 'utf8' }).trim();
const insert = (table, values) => sql(`INSERT INTO "${table}" (${Object.keys(values).map(k => `"${k}"`).join(',')}) VALUES (${Object.values(values).map(lit).join(',')});`);
const row = id => JSON.parse(sql(`SELECT row_to_json(c) FROM "Cremaciones" c WHERE "Id"=${lit(id)};`));
const snapshot = () => sql(`SELECT json_build_object(${['Cremaciones', 'ComentariosOperativosCremacion',
  'CambiosInstruccionesCremacion', 'CambiosAccesorioCremacion', 'EventosArchivoCremacion',
  'VerificacionesInicioCremacion', 'TransicionesEstadoCremacion'].map(t =>
  `${lit(t)},(SELECT coalesce(json_agg(x ORDER BY x."Id"),'[]') FROM "${t}" x)`).join(',')});`);
async function api(path, method = 'GET', body, token, expected = 200, base = url) {
  const response = await fetch(`${base}/api/${path}`, {
    method, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  assert.equal(response.status, expected, `${method} ${path}: ${response.status} ${text.includes('"token"') ? "[access token omitted]" : text.slice(0, 500)}`);
  return text ? JSON.parse(text) : null;
}
const login = async (actor, expected = 200, base = url) =>
  api('auth/login', 'POST', { email: actor.email, password }, undefined, expected, base);
const decode = token => JSON.parse(Buffer.from(token.split('.')[1], 'base64url'));

if (process.argv[2] === 'seed') {
  assert.equal(sql('SELECT count(*) FROM "Usuarios";'), '0', 'Fresh isolated database required');
  const s = { actors: {}, cases: {} };
  for (const name of ['owner', 'inactive', 'removed', 'added', 'changed', 'admin', 'fakeOwner', 'missing']) {
    const email = `c6a-${name}@example.invalid`;
    const actor = await api('auth/register', 'POST', { firstName: name, lastName: 'QA', email, password });
    s.actors[name] = { id: actor.userId, email };
  }
  sql(`UPDATE "Usuarios" SET "EsOwner"=true WHERE "Id"=${lit(s.actors.owner.id)};`);
  const ownerToken = (await login(s.actors.owner)).token;
  const permissions = await api('permissions', 'GET', undefined, ownerToken);
  s.manage = permissions.find(p => p.code === 'Cremations.Manage').id;
  s.view = permissions.find(p => p.code === 'Cremations.View').id;
  for (const name of ['inactive', 'removed', 'added', 'changed', 'admin', 'fakeOwner']) {
    const actor = s.actors[name];
    const role = await api('permissions/roles', 'POST', { name: name === 'fakeOwner' ? 'Owner' : `C6A ${name}` }, ownerToken);
    actor.role = role.id;
    await api(`permissions/roles/${role.id}/permissions`, 'PUT', [name === 'added' ? s.view : s.manage], ownerToken, 204);
    await api(`permissions/users/${actor.id}/roles`, 'PUT', name === 'admin' ? [adminId, role.id] : [role.id], ownerToken, 204);
  }
  for (const actor of Object.values(s.actors)) actor.token = (await login(actor)).token;
  const now = new Date().toISOString();
  const customer = randomUUID(); s.packageId = randomUUID();
  insert('PaquetesCremacion', { Id: s.packageId, Nombre: 'C6A package', TipoPaquete: 1, IncluyeUrna: false,
    IncluyeHuella: true, DescripcionAccesorio: 'Original', IncluyeCertificado: false, EsPublico: false,
    OrdenVisualizacion: 0, Activo: true, FechaCreacion: now });
  insert('Clientes', { Id: customer, Nombre: 'QA', ApellidoPaterno: 'C6A', Telefono: '000',
    CorreoElectronico: 'c6a-client@example.invalid', Activo: true, FechaCreacion: now });
  for (const [name, status] of [['pending', 1], ['status', 1], ['delivered', 8]]) {
    const pet = randomUUID(), reception = randomUUID(), id = randomUUID(); s.cases[name] = id;
    insert('Mascotas', { Id: pet, CustomerId: customer, Nombre: name, Especie: 'Canino', Raza: 'QA', Sexo: 'M',
      Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-10-01', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: s.actors.owner.id,
      FechaRecepcion: now, CodigoQr: `C6A-${name}`, PesoVerificadoKg: 5, TieneObjetosPersonales: false,
      Activo: true, FechaCreacion: now, NombreClienteSnapshot: 'QA C6A', NombreMascotaSnapshot: name });
    insert('Cremaciones', { Id: id, RecepcionId: reception, AsignadoAUsuarioId: s.actors.owner.id,
      PaqueteCremacionId: s.packageId, TipoCremacion: 1, Estado: status, NombrePaquete: 'C6A package',
      IncluyeUrna: false, IncluyeHuella: true, IncluyeCertificado: false, InstruccionesEspeciales: 'Original',
      DescripcionAccesorio: 'Original', Notas: 'Original', PrecioCotizado: 300, MontoPagoRequeridoInicio: 0,
      PesoCotizadoKg: 5, PesoMinimoCotizadoKg: 0, PesoMaximoCotizadoKg: 5, Activo: true, FechaCreacion: now });
  }
  writeFileSync(statePath, JSON.stringify(s), { mode: 0o600 });
  console.log('Seed PASS: real login JWTs and isolated fixtures');
  process.exit(0);
}
const s = JSON.parse(readFileSync(statePath));
const owner = s.actors.owner;
const read = (token, expected = 200) => api(`Cremations/${s.cases.pending}`, 'GET', undefined, token, expected);
const putBody = notes => ({ assignedToUserId: owner.id, cremationPackageId: s.packageId, urnId: null,
  scheduledAt: null, specialInstructions: 'Original', accessoryDescription: 'Original', notes });
async function put(token, expected = 200) {
  const notes = `C6A ${randomUUID()}`, before = snapshot();
  await api(`Cremations/${s.cases.pending}`, 'PUT', putBody(notes), token, expected);
  if (expected === 200) assert.equal(row(s.cases.pending).Notas, notes);
  else assert.equal(snapshot(), before);
}
async function setRoles(actor, roles) {
  await api(`permissions/users/${actor.id}/roles`, 'PUT', roles, owner.token, 204);
  assert.deepEqual(JSON.parse(sql(`SELECT coalesce(json_agg("RoleId" ORDER BY "RoleId"),'[]') FROM "UsuarioRoles" WHERE "UserId"=${lit(actor.id)};`)), [...roles].sort());
}
async function setPermissions(role, permissions) {
  await api(`permissions/roles/${role}/permissions`, 'PUT', permissions, owner.token, 204);
  assert.deepEqual(JSON.parse(sql(`SELECT coalesce(json_agg("PermissionId" ORDER BY "PermissionId"),'[]') FROM "RolPermisos" WHERE "RoleId"=${lit(role)};`)), [...permissions].sort());
}
function active(actor, value) {
  // No user deactivation API exists: change only the explicitly disposable fixture.
  sql(`UPDATE "Usuarios" SET "Activo"=${value} WHERE "Id"=${lit(actor.id)};`);
  assert.equal(sql(`SELECT "Activo" FROM "Usuarios" WHERE "Id"=${lit(actor.id)};`), value ? 't' : 'f');
}
async function amendments(token, expected) {
  for (const [route, field, column, table] of [
    ['instructions-amendments', 'SpecialInstructions', 'InstruccionesEspeciales', 'CambiosInstruccionesCremacion'],
    ['accessory-amendments', 'AccessoryDescription', 'DescripcionAccesorio', 'CambiosAccesorioCremacion'],
  ]) {
    const before = snapshot(), previous = row(s.cases.delivered)[column], value = randomUUID();
    const count = Number(sql(`SELECT count(*) FROM "${table}";`));
    await api(`Cremations/${s.cases.delivered}/${route}`, 'POST', { requestId: randomUUID(), reason: 'Verificación C6A',
      [`expectedCurrent${field}`]: previous, [`new${field}`]: value }, token, expected);
    if (expected === 200) {
      assert.equal(row(s.cases.delivered)[column], value);
      assert.equal(Number(sql(`SELECT count(*) FROM "${table}";`)), count + 1);
    } else assert.equal(snapshot(), before);
  }
}
async function rejectEndpoints(token, expected) {
  const before = snapshot();
  await read(token, expected); await put(token, expected);
  await api(`Cremations/${s.cases.status}/status`, 'PATCH', { status: 9 }, token, expected);
  await amendments(token, expected);
  assert.equal(snapshot(), before, 'No business mutation/history on rejected requests');
}
if (process.argv[2] === 'expiry') {
  const expiredUrl = process.env.C6A_QA_EXPIRED_URL;
  assert.match(expiredUrl ?? '', /^http:\/\/127\.0\.0\.1:\d+$/);
  const expired = (await login(owner, 200, expiredUrl)).token;
  assert.ok(decode(expired).exp < Date.now() / 1000 - 550);
  await read(expired, 401);
  const pieces = owner.token.split('.');
  pieces[2] = `${pieces[2][0] === 'A' ? 'B' : 'A'}${pieces[2].slice(1)}`;
  await read(pieces.join('.'), 401);
  console.log('C6A-12 PASS: expired real-login and tampered-signature tokens rejected401');
} else if (process.argv[2] === 'failure') {
  const before = snapshot();
  // Break only authorization resolution: business reads/ordinary PUT do not need this table.
  sql('ALTER TABLE "RolPermisos" RENAME TO "RolPermisos_C6A_unavailable";');
  try {
    const response = await read(owner.token, 500);
    assert.equal(response.success, false);
    assert.ok(response.message.includes('error interno'));
    await put(owner.token, 500);
    assert.equal(snapshot(), before);
    console.log('C6A-14 PASS: DB auth failure yields controlled500; no stale-claim fallback/mutation');
  } finally { sql('ALTER TABLE "RolPermisos_C6A_unavailable" RENAME TO "RolPermisos";'); }
  await read(owner.token);
} else {
  assert.equal(process.argv[2], 'verify');
  await setPermissions(s.actors.added.role, [s.view]);
  await setPermissions(s.actors.changed.role, [s.manage]);
  await setRoles(s.actors.admin, [adminId, s.actors.admin.role]);
  await read(s.actors.inactive.token); await put(s.actors.inactive.token);
  console.log('C6A-1 PASS active GET/PUT200 with persisted write');
  active(s.actors.inactive, false);
  try {
    const result = await login(s.actors.inactive, 401);
    assert.equal(result.token, undefined); assert.match(result.message, /contraseña/);
    console.log('C6A-2 PASS inactive login401, no token');
    await rejectEndpoints(s.actors.inactive.token, 401);
    console.log('C6A-3/C6A-11 PASS inactive original token: read/PUT/status/amendments401, no mutation');
  } finally { active(s.actors.inactive, true); }
  await setRoles(s.actors.removed, []);
  try {
    await rejectEndpoints(s.actors.removed.token, 403);
    console.log('C6A-4/C6A-11 PASS removed membership: all endpoints403, no mutation');
  } finally { await setRoles(s.actors.removed, [s.actors.removed.role]); }
  await put(s.actors.added.token, 403);
  await setPermissions(s.actors.added.role, [s.view, s.manage]);
  await put(s.actors.added.token);
  console.log('C6A-5 PASS same View-only token now PUT200 after grant');
  await setPermissions(s.actors.changed.role, [s.view]);
  await put(s.actors.changed.token, 403); await read(s.actors.changed.token);
  await setPermissions(s.actors.changed.role, [s.manage]); await put(s.actors.changed.token);
  await api(`permissions/roles/${s.actors.changed.role}`, 'PUT', { name: 'C6A changed', isActive: false }, owner.token, 204);
  assert.equal(sql(`SELECT "IsActive" FROM "Roles" WHERE "Id"=${lit(s.actors.changed.role)};`), 'f');
  await read(s.actors.changed.token, 403);
  await api(`permissions/roles/${s.actors.changed.role}`, 'PUT', { name: 'C6A changed', isActive: true }, owner.token, 204);
  console.log('C6A-6 PASS role grant removal/addition and role deactivation reflected immediately');
  await amendments(s.actors.admin.token, 200);
  await setRoles(s.actors.admin, [s.actors.admin.role]);
  assert.equal(decode(s.actors.admin.token).pcms_protected_admin, 'true');
  await amendments(s.actors.admin.token, 403);
  console.log('C6A-7 PASS stale protected-Admin token rejected after membership removal, no history');
  const preAdminToken = (await login(s.actors.admin)).token;
  assert.equal(decode(preAdminToken).pcms_protected_admin, undefined);
  await setRoles(s.actors.admin, [adminId, s.actors.admin.role]);
  await amendments(preAdminToken, 200);
  console.log('C6A-8 PASS same pre-Admin token succeeds after authoritative membership addition');
  assert.equal(decode(s.actors.fakeOwner.token).pcms_owner, undefined);
  await amendments(s.actors.fakeOwner.token, 403);
  await api(`permissions/roles/${s.actors.fakeOwner.role}`, 'PUT', { name: 'Protected Admin', isActive: true }, owner.token, 204);
  await amendments(s.actors.fakeOwner.token, 403);
  await api(`permissions/roles/${s.actors.fakeOwner.role}`, 'PUT', { name: 'Owner', isActive: true }, owner.token, 204);
  console.log('C6A-9 PASS custom Owner/Admin-like roles+Manage are not authoritative privileges');
  await amendments(owner.token, 200);
  console.log('C6A-10 PASS active authoritative Owner, exactly one row per valid amendment');
  // Fixture-only flag change exercises stale Owner claim replacement, not a new demotion API.
  sql(`UPDATE "Usuarios" SET "EsOwner"=false WHERE "Id"=${lit(owner.id)};`);
  try {
    assert.equal(sql(`SELECT "EsOwner" FROM "Usuarios" WHERE "Id"=${lit(owner.id)};`), 'f');
    await rejectEndpoints(owner.token, 403);
  } finally { sql(`UPDATE "Usuarios" SET "EsOwner"=true WHERE "Id"=${lit(owner.id)};`); }
  console.log('Additional PASS: stale Owner flag grants no server authority');
  // No business rows reference this disposable registered user.
  sql(`DELETE FROM "UsuarioRoles" WHERE "UserId"=${lit(s.actors.missing.id)}; DELETE FROM "Usuarios" WHERE "Id"=${lit(s.actors.missing.id)};`);
  assert.equal(sql(`SELECT count(*) FROM "Usuarios" WHERE "Id"=${lit(s.actors.missing.id)};`), '0');
  await read(s.actors.missing.token, 401);
  await read(undefined, 401);
  assert.ok((await login(owner)).token);
  await api('auth/login', 'POST', { email: owner.email, password: 'Wrong password' }, undefined, 401);
  await api('auth/login', 'POST', { email: 'absent@example.invalid', password }, undefined, 401);
  const registered = await api('auth/register', 'POST', { firstName: 'Anonymous', lastName: 'QA',
    email: `c6a-public-${randomUUID()}@example.invalid`, password });
  assert.ok(registered.token); assert.equal(decode(registered.token).sub, registered.userId);
  await read(registered.token, 403);
  console.log('C6A-13 PASS anonymous protected401, login/register200; invalid credentials uniform401');
  console.log('C6A verify PASS');
}
