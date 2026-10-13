// Real API/SQL regression. Use only a fresh disposable PostgreSQL database.
// Apply migrations, start the API, then set C3F3_QA_CONTAINER=pcms-c3f3-*,
// C3F3_QA_DATABASE=pcms_c3f3_*, C3F3_QA_URL=http://127.0.0.1:<port>,
// C3F3_QA_STATE=<temporary JSON path>. Run seed, red before the fix, then verify.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C3F3_QA_CONTAINER;
const database = process.env.C3F3_QA_DATABASE;
const url = process.env.C3F3_QA_URL;
const statePath = process.env.C3F3_QA_STATE;
assert.match(container ?? '', /^pcms-c3f3-/);
assert.match(database ?? '', /^pcms_c3f3_/);
assert.match(url ?? '', /^http:\/\/127\.0\.0\.1:\d+$/);
assert.ok(statePath, 'Explicit temporary state path required');
const password = 'Disposable-C3F3-password-42!';
const literal = value => value === null ? 'NULL' : `'${String(value).replaceAll("'", "''")}'`;
function sql(query) {
  return execFileSync('docker', ['exec', '-i', container, 'psql', '-X', '-v',
    'ON_ERROR_STOP=1', '-U', 'postgres', '-d', database, '-At'],
  { input: query, encoding: 'utf8' }).trim();
}
const insert = (table, values) => sql(`INSERT INTO "${table}" (${Object.keys(values).map(x => `"${x}"`).join(',')}) VALUES (${Object.values(values).map(literal).join(',')});`);
const tables = ['Cremaciones', 'CambiosInstruccionesCremacion', 'CambiosAccesorioCremacion',
  'ReasignacionesCremacion', 'EventosArchivoCremacion', 'AclaracionesCremacion'];
const snapshot = () => JSON.parse(sql(`SELECT json_build_object(${tables.map(t =>
  `${literal(t)}, (SELECT coalesce(json_agg(t ORDER BY t."Id"),'[]') FROM "${t}" t)`).join(',')});`));
async function request(path, method = 'GET', body, token) {
  const response = await fetch(`${url}/api/${path}`, { method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  return { status: response.status, data: text ? JSON.parse(text) : null };
}
async function api(path, method, body, token, expected = 200) {
  const result = await request(path, method, body, token);
  assert.equal(result.status, expected, `${method} ${path}: ${JSON.stringify(result)}`);
  return result.data;
}
const login = async actor => (await api('auth/login', 'POST', { email: actor.email, password })).token;
const message = response => Object.values(response.data.errors ?? {}).flat().join(' | ');

if (process.argv[2] === 'seed') {
  assert.equal(sql('SELECT count(*) FROM "Usuarios";'), '0', 'Fresh disposable database required');
  const s = { actors: {}, cases: {} };
  for (const name of ['owner', 'manager', 'custom']) {
    const email = `c3f3-${name}@example.invalid`;
    const user = await api('auth/register', 'POST', { firstName: name, lastName: 'QA', email, password });
    s.actors[name] = { id: user.userId, email };
  }
  sql(`UPDATE "Usuarios" SET "EsOwner"=true WHERE "Id"=${literal(s.actors.owner.id)};`);
  const owner = await login(s.actors.owner);
  const permissions = await api('permissions', 'GET', undefined, owner);
  const manageId = permissions.find(p => p.code === 'Cremations.Manage').id;
  for (const [actor, name] of [['manager', 'Manager'], ['custom', 'C3F3 Custom Team']]) {
    const role = await api('permissions/roles', 'POST', { name }, owner);
    await api(`permissions/roles/${role.id}/permissions`, 'PUT', [manageId], owner, 204);
    await api(`permissions/users/${s.actors[actor].id}/roles`, 'PUT', [role.id], owner, 204);
  }
  const now = '2026-10-13T12:00:00Z';
  const customer = randomUUID();
  insert('Clientes', { Id: customer, Nombre: 'Cliente', ApellidoPaterno: 'C3F3', Telefono: '000',
    CorreoElectronico: 'c3f3-client@example.invalid', Activo: true, FechaCreacion: now });
  for (const [name, status] of [['active', 3], ['archive', 6]]) {
    const pet = randomUUID(); const reception = randomUUID(); const id = randomUUID();
    s.cases[name] = id;
    insert('Mascotas', { Id: pet, CustomerId: customer, Nombre: `C3F3 ${name}`, Especie: 'Canino',
      Raza: 'QA', Sexo: 'M', Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-10-01', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: s.actors.owner.id,
      FechaRecepcion: now, CodigoQr: `C3F3-${name}`, PesoVerificadoKg: 5,
      TieneObjetosPersonales: false, Activo: true, FechaCreacion: now,
      NombreClienteSnapshot: 'Cliente C3F3', NombreMascotaSnapshot: `C3F3 ${name}` });
    insert('Cremaciones', { Id: id, RecepcionId: reception, AsignadoAUsuarioId: s.actors.owner.id,
      TipoCremacion: 1, Estado: status, NombrePaquete: 'Servicio histórico', IncluyeUrna: false,
      IncluyeHuella: true, IncluyeCertificado: false, FechaInicio: now,
      FechaFinalizacion: status === 6 ? now : null, InstruccionesEspeciales: 'Instrucciones iniciales',
      DescripcionAccesorio: 'Accesorio inicial', Notas: 'Nota original', PrecioCotizado: 300,
      MontoPagoRequeridoInicio: 0, PesoCotizadoKg: 5, PesoMinimoCotizadoKg: 0,
      PesoMaximoCotizadoKg: 5, Activo: true, FechaCreacion: now });
  }
  writeFileSync(statePath, JSON.stringify(s, null, 2), { mode: 0o600 });
  console.log('Seed PASS: disposable Owner, Manager, custom actor and C3 cases');
} else {
  const s = JSON.parse(readFileSync(statePath, 'utf8'));
  const tokens = Object.fromEntries(await Promise.all(Object.entries(s.actors).map(async ([name, actor]) => [name, await login(actor)])));
  const id = s.cases.active;
  const cases = [
    ['L1 instructions missing reason', `cremations/${id}/instructions-amendments`, 'POST',
      { requestId: randomUUID(), newSpecialInstructions: 'Nuevo texto', expectedCurrentSpecialInstructions: 'Instrucciones iniciales' }],
    ['L1 instructions long reason', `cremations/${id}/instructions-amendments`, 'POST',
      { requestId: randomUUID(), newSpecialInstructions: 'Nuevo texto', reason: 'x'.repeat(1001) }],
    ['L1 instructions long text', `cremations/${id}/instructions-amendments`, 'POST',
      { requestId: randomUUID(), newSpecialInstructions: 'x'.repeat(1001), reason: 'Motivo válido' }],
    ['L2 accessory missing reason', `cremations/${id}/accessory-amendments`, 'POST',
      { requestId: randomUUID(), newAccessoryDescription: 'Nuevo accesorio', expectedCurrentAccessoryDescription: 'Accesorio inicial' }],
    ['L2 accessory long text', `cremations/${id}/accessory-amendments`, 'POST',
      { requestId: randomUUID(), newAccessoryDescription: 'x'.repeat(501), reason: 'Motivo válido' }],
    ['L3 archive missing reason', `cremations/${s.cases.archive}`, 'DELETE', { requestId: randomUUID() }],
    ['L3 restore missing reason', `cremations/${s.cases.archive}/restore`, 'PATCH', { requestId: randomUUID() }],
    ['L4 reassignment missing reason', `cremations/${id}/reassignments`, 'POST',
      { requestId: randomUUID(), newAssignedToUserId: s.actors.manager.id }],
    ['L4 clarification missing text', `cremations/${id}/clarifications`, 'POST', { requestId: randomUUID() }],
  ];
  const before = snapshot();
  for (const [name, path, method, body] of cases) {
    const result = await request(path, method, body, tokens.owner);
    assert.equal(result.status, 400, `${name}: rejection status unchanged`);
    assert.deepEqual(snapshot(), before, `${name}: no mutation or evidence`);
    const error = message(result);
    assert.ok(error, `${name}: field validation message`);
    if (process.argv[2] === 'red' && name === 'L1 instructions missing reason') {
      assert.match(error, /^El motivo .*obligatorio\.$/, `Expected red failure, received: ${error}`);
    }
    if (process.argv[2] === 'verify') {
      assert.match(error, /[ÁÉÍÓÚáéíóúñ]|El |La |Las /, `${name}: Spanish validation`);
      assert.doesNotMatch(error, /\b(The|field|must|maximum|length|required|is)\b/i, `${name}: no framework English`);
    }
  }
  if (process.argv[2] === 'red') throw new Error('Baseline unexpectedly emitted Spanish required-reason text');
  assert.equal(process.argv[2], 'verify');
  console.log('L1–L4 PASS: same HTTP 400, Spanish field errors, no mutation/history');
  let current = 'Instrucciones iniciales';
  for (const [actor, next] of [['manager', 'Cambio gerente'], ['owner', 'Cambio propietario'], ['custom', 'Cambio personalizado']]) {
    await api(`cremations/${id}/instructions-amendments`, 'POST', {
      requestId: randomUUID(), expectedCurrentSpecialInstructions: current,
      newSpecialInstructions: next, reason: 'Motivo válido',
    }, tokens[actor]);
    current = next;
  }
  await api(`cremations/${id}/accessory-amendments`, 'POST', {
    requestId: randomUUID(), expectedCurrentAccessoryDescription: 'Accesorio inicial',
    newAccessoryDescription: 'Accesorio actualizado', reason: 'Motivo válido',
  }, tokens.owner);
  await api(`cremations/${s.cases.archive}`, 'DELETE', {
    requestId: randomUUID(), reason: 'Motivo válido',
  }, tokens.owner, 204);
  const after = snapshot();
  assert.equal(after.Cremaciones.find(c => c.Id === id).InstruccionesEspeciales, current);
  assert.equal(after.Cremaciones.find(c => c.Id === id).DescripcionAccesorio, 'Accesorio actualizado');
  assert.equal(after.Cremaciones.find(c => c.Id === s.cases.archive).Activo, false);
  const roles = after.CambiosInstruccionesCremacion.sort((a, b) => a.Secuencia - b.Secuencia).map(x => x.RolActor);
  assert.deepEqual(roles, ['Manager', 'Owner', 'C3F3 Custom Team'], 'Persisted historical role names unchanged');
  assert.equal(after.CambiosAccesorioCremacion.length, 1);
  assert.equal(after.EventosArchivoCremacion.length, 1);
  console.log('L5 PASS: valid instructions/accessory/archive, immutable role snapshots and evidence');
}
