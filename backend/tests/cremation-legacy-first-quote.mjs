// Real API/PostgreSQL regression. Run ONLY against a fresh disposable database.
// Apply migrations and start the API, then set C3F2_QA_CONTAINER=pcms-c3f2-*,
// C3F2_QA_DATABASE=pcms_c3f2_*, C3F2_QA_URL=http://127.0.0.1:<port>,
// C3F2_QA_STATE=<temporary JSON path>. Run seed, red (before fix), then verify.
// Account creation remains a separate existing operation; this test does not
// introduce automatic creation or change the snapshots that operation stores.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';

const container = process.env.C3F2_QA_CONTAINER;
const database = process.env.C3F2_QA_DATABASE;
const url = process.env.C3F2_QA_URL;
const statePath = process.env.C3F2_QA_STATE;
assert.match(container ?? '', /^pcms-c3f2-/);
assert.match(database ?? '', /^pcms_c3f2_/);
assert.match(url ?? '', /^http:\/\/127\.0\.0\.1:\d+$/);
assert.ok(statePath, 'Explicit temporary state path required');
const password = 'Disposable-C3F2-password-42!';
const literal = x => x === null ? 'NULL' : `'${String(x).replaceAll("'", "''")}'`;
function sql(query) {
  return execFileSync('docker', ['exec', '-i', container, 'psql', '-X', '-v',
    'ON_ERROR_STOP=1', '-U', 'postgres', '-d', database, '-At'],
  { input: query, encoding: 'utf8' }).trim();
}
const insert = (table, values) => sql(`INSERT INTO "${table}" (${Object.keys(values).map(x => `"${x}"`).join(',')}) VALUES (${Object.values(values).map(literal).join(',')});`);
const histories = ['VerificacionesInicioCremacion', 'RevalidacionesPagoInicioCremacion',
  'ComentariosOperativosCremacion', 'ReasignacionesCremacion', 'CambiosInstruccionesCremacion',
  'CambiosAccesorioCremacion', 'EventosArchivoCremacion', 'AclaracionesCremacion'];
const snapshot = () => JSON.parse(sql(`SELECT json_build_object(${['Cremaciones', 'CuentasPago', 'Pagos', 'Recolecciones', ...histories]
  .map(t => `${literal(t)}, (SELECT coalesce(json_agg(t ORDER BY t."Id"),'[]') FROM "${t}" t)`).join(',')});`));
async function request(path, method = 'GET', body, token) {
  const response = await fetch(`${url}/api/${path}`, { method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await response.text();
  let data;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  return { status: response.status, data };
}
async function api(path, method, body, token, expected = 200) {
  const r = await request(path, method, body, token);
  assert.equal(r.status, expected, `${method} ${path}: ${r.status} ${JSON.stringify(r.data).slice(0,400)}`);
  return r.data;
}
const login = async actor => (await api('auth/login', 'POST', { email: actor.email, password })).token;

if (process.argv[2] === 'seed') {
  assert.equal(sql('SELECT count(*) FROM "Usuarios";'), '0', 'Fresh disposable database required');
  const s = { cases: {}, packages: {}, actors: {} };
  for (const name of ['owner', 'ordinary']) {
    const email = `c3f2-${name}@example.invalid`;
    const actor = await api('auth/register', 'POST', { firstName: name, lastName: 'QA', email, password });
    s.actors[name] = { id: actor.userId, email };
  }
  sql(`UPDATE "Usuarios" SET "EsOwner"=true WHERE "Id"=${literal(s.actors.owner.id)};`);
  const now = '2026-10-01T12:00:00Z';
  const customer = randomUUID();
  insert('Clientes', { Id: customer, Nombre: 'Cliente', ApellidoPaterno: 'C3F2', Telefono: '000',
    CorreoElectronico: 'c3f2-client@example.invalid', Activo: true, FechaCreacion: now });
  for (const [name, includes, amount, active, priceActive] of [
    ['accessory', true, 850, true, true], ['plain', false, 600, true, true],
    ['inactive', true, 900, false, true], ['inactivePrice', true, 950, true, false],
  ]) {
    const id = randomUUID(); const price = randomUUID();
    s.packages[name] = { id, price, amount, includes, name: `C3F2 ${name}` };
    insert('PaquetesCremacion', { Id: id, Nombre: `C3F2 ${name}`, TipoPaquete: 1,
      IncluyeUrna: false, IncluyeHuella: includes, DescripcionAccesorio: includes ? 'Huella de recuerdo' : null,
      IncluyeCertificado: true, EsPublico: true, OrdenVisualizacion: 0, Activo: active, FechaCreacion: now });
    insert('PreciosCremacion', { Id: price, PaqueteCremacionId: id, TipoCremacion: 1,
      PesoMinimoKg: 0, PesoMaximoKg: 5, Precio: amount, MontoPagoRequeridoRecoleccion: 200,
      EsPublico: true, Activo: priceActive, FechaCreacion: now });
  }
  for (const [name, status] of [
    ['accessory',6], ['plain',6], ['account',6], ['package',6], ['quote',6], ['invalid',6],
    ['atomic',6], ['concurrent',6], ['browser',6], ['nullDefault',6], ['history',6],
    ['inProgress',3], ['cooling',4], ['processing',5], ['ready',7], ['delivered',8], ['cancelled',9],
  ]) {
    const pet = randomUUID(); const reception = randomUUID(); const id = randomUUID();
    s.cases[name] = id;
    insert('Mascotas', { Id: pet, CustomerId: customer, Nombre: `C3F2 ${name}`, Especie: 'Canino',
      Raza: 'QA', Sexo: 'M', Color: 'QA', PesoKg: 5, FechaFallecimiento: '2026-09-30', Activo: true, FechaCreacion: now });
    insert('Recepciones', { Id: reception, MascotaId: pet, RecibidoPorUsuarioId: s.actors.owner.id,
      FechaRecepcion: now, CodigoQr: `C3F2-${name}`, PesoVerificadoKg: 5,
      TieneObjetosPersonales: false, Activo: true, FechaCreacion: now,
      NombreClienteSnapshot: 'Cliente C3F2', NombreMascotaSnapshot: `C3F2 ${name}` });
    insert('Cremaciones', { Id: id, RecepcionId: reception, AsignadoAUsuarioId: s.actors.owner.id,
      TipoCremacion: 1, Estado: status, NombrePaquete: '', IncluyeUrna: false, IncluyeHuella: false,
      IncluyeCertificado: false, FechaInicio: now, FechaFinalizacion: status >= 6 ? now : null,
      InstruccionesEspeciales: 'Instrucciones históricas', DescripcionAccesorio: null,
      Notas: 'Nota histórica', PrecioCotizado: name === 'quote' ? 300 : null,
      MontoPagoRequeridoInicio: 125, Activo: true, FechaCreacion: now });
    insert('VerificacionesInicioCremacion', { Id: randomUUID(), CremacionId: id, RecepcionId: reception,
      CodigoQrRecepcionSnapshot: `C3F2-${name}`, ConfirmadoPorUsuarioId: s.actors.owner.id,
      NombreUsuarioConfirmoSnapshot: 'owner QA', FechaConfirmacion: now, SolicitudId: randomUUID() });
    insert('ComentariosOperativosCremacion', { Id: randomUUID(), CremacionId: id, Estado: 3,
      Comentario: 'Evidencia previa', CreadoPorUsuarioId: s.actors.owner.id,
      NombreUsuarioSnapshot: 'owner QA', FechaCreacion: now });
  }
  sql(`UPDATE "Cremaciones" SET "PaqueteCremacionId"=${literal(s.packages.accessory.id)},
    "IncluyeHuella"=true,"DescripcionAccesorio"='Accesorio previo' WHERE "Id"=${literal(s.cases.package)};`);
  insert('CuentasPago', { Id: randomUUID(), CremacionId: s.cases.account, TotalServicio: 300,
    RequiereRevisionFinanciera: false, FechaCreacion: now });
  // A separate established case carries pre-existing histories/payment evidence.
  const historicalId = s.cases.history;
  sql(`UPDATE "Cremaciones" SET "PaqueteCremacionId"=${literal(s.packages.accessory.id)},
    "PrecioCotizado"=850,"IncluyeHuella"=true,"DescripcionAccesorio"='Accesorio previo'
    WHERE "Id"=${literal(historicalId)};`);
  const evidence = () => ({ Id: randomUUID(), CremacionId: historicalId, Secuencia: 1,
    RequestId: randomUUID(), ActorUsuarioId: s.actors.owner.id, NombreActorSnapshot: 'owner QA',
    RolActor: 'Owner', Motivo: 'Evidencia histórica QA', Estado: 6, FechaCreacion: now });
  insert('CambiosAccesorioCremacion', { ...evidence(), DescripcionAnterior: 'Inicial', DescripcionNueva: 'Accesorio previo' });
  insert('CambiosInstruccionesCremacion', { ...evidence(), InstruccionesAnteriores: 'Inicial', InstruccionesNuevas: 'Instrucciones históricas' });
  insert('ReasignacionesCremacion', { ...evidence(), AnteriorUsuarioId: null, NombreAnteriorSnapshot: null,
    NuevoUsuarioId: s.actors.owner.id, NombreNuevoSnapshot: 'owner QA' });
  insert('EventosArchivoCremacion', { ...evidence(), ActivoAnterior: false, ActivoNuevo: true });
  const clarification = evidence(); delete clarification.Motivo;
  insert('AclaracionesCremacion', { ...clarification, Texto: 'Aclaración previa' });
  const historicalAccount = randomUUID();
  insert('CuentasPago', { Id: historicalAccount, CremacionId: historicalId, TotalServicio: 850,
    PaqueteCremacionId: s.packages.accessory.id, NombrePaqueteSnapshot: s.packages.accessory.name,
    RequiereRevisionFinanciera: false, FechaCreacion: now });
  insert('Pagos', { Id: randomUUID(), CuentaPagoId: historicalAccount, RegistradoPorUsuarioId: s.actors.owner.id,
    Monto: 100, MetodoPago: 1, FechaPago: now, FechaCreacion: now });
  writeFileSync(statePath, JSON.stringify(s, null, 2), { mode: 0o600 });
  console.log('Seed PASS: isolated legacy fixtures, packages/prices, C1A/C3 evidence');
} else {
  const s = JSON.parse(readFileSync(statePath, 'utf8'));
  const token = await login(s.actors.owner);
  const before = snapshot();
  const cremation = (state, name) => state.Cremaciones.find(c => c.Id === s.cases[name]);
  const payload = (name, pack = 'accessory', description = null) => ({
    assignedToUserId: s.actors.owner.id, cremationPackageId: s.packages[pack].id,
    urnId: null, scheduledAt: null, specialInstructions: 'Instrucciones históricas',
    notes: 'Nota histórica', accessoryDescription: description,
  });
  const update = (name, pack = 'accessory', description = null, expected = 200) =>
    api(`cremations/${s.cases[name]}`, 'PUT', payload(name, pack, description), token, expected);
  const reject = async (name, pack = 'accessory', description = null, expected = 409) => {
    const prior = snapshot();
    const result = await update(name, pack, description, expected);
    assert.deepEqual(snapshot(), prior, `${name}: rejection must not mutate data/history`);
    return result;
  };
  if (process.argv[2] === 'red') {
    // The same success expectation used after the fix must fail on the baseline.
    const result = await request(`cremations/${s.cases.accessory}`, 'PUT', payload('accessory'), token);
    if (result.status !== 200) assert.deepEqual(snapshot(), before, 'Red rejection leaves no partial state');
    assert.equal(result.status, 200, JSON.stringify(result.data));
    throw new Error('Baseline unexpectedly accepts the accessory first quote');
  }
  assert.equal(process.argv[2], 'verify');
  for (const pack of ['accessory', 'plain']) {
    const quote = await api(`CremationPricing/quote?cremationPackageId=${s.packages[pack].id}&weightKg=5`, 'GET', undefined, token);
    assert.equal(quote.cremationPriceId, s.packages[pack].price);
    assert.equal(quote.price, s.packages[pack].amount);
    assert.equal(quote.requiredCollectionPaymentAmount, 200);
  }
  const a = await update('accessory');
  assert.equal(a.accessoryDescription, 'Huella de recuerdo');
  assert.equal(a.includesPawPrint, true);
  assert.equal(a.status, 6);
  const plain = await update('plain', 'plain', 'ignored for non-accessory');
  assert.equal(plain.accessoryDescription, null);
  assert.equal(plain.includesPawPrint, false);
  console.log('F2-2 / F2-3 PASS: accessory default established; non-accessory unchanged');
  await reject('account');
  await reject('package', 'accessory', 'ordinary bypass');
  await reject('quote');
  await reject('accessory', 'accessory', 'ordinary bypass');
  console.log('F2-4 / F2-5 / F2-6 / F2-7 PASS: exclusions and subsequent guard');
  const requestId = randomUUID();
  await api(`cremations/${s.cases.accessory}/accessory-amendments`, 'POST', {
    requestId, expectedCurrentAccessoryDescription: 'Huella de recuerdo',
    newAccessoryDescription: '  Huella corregida  ', reason: '  Corrección QA  ',
  }, token);
  const amendment = snapshot().CambiosAccesorioCremacion.filter(x => x.CremacionId === s.cases.accessory);
  assert.equal(amendment.length, 1);
  assert.equal(amendment[0].SolicitudId ?? amendment[0].RequestId, requestId);
  assert.equal(amendment[0].Secuencia, 1);
  assert.equal(amendment[0].DescripcionAnterior, 'Huella de recuerdo');
  assert.equal(amendment[0].DescripcionNueva, 'Huella corregida');
  assert.equal(amendment[0].ActorUsuarioId, s.actors.owner.id);
  assert.equal(amendment[0].Motivo, 'Corrección QA');
  console.log('F2-8 PASS: one normal B3 amendment after establishment');
  await reject('invalid', 'inactive');
  await reject('invalid', 'inactivePrice');
  const missing = snapshot();
  await api(`cremations/${s.cases.invalid}`, 'PUT', { ...payload('invalid'), cremationPackageId: randomUUID() }, token, 409);
  assert.deepEqual(snapshot(), missing);
  console.log('F2-9 PASS: invalid/inactive selection rejects without partial state');
  // Force failure at COMMIT, after the UPDATE has reached PostgreSQL.
  const atomicBefore = snapshot();
  sql(`CREATE FUNCTION c3f2_fail_commit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
    IF NEW."Id"=${literal(s.cases.atomic)}::uuid THEN RAISE EXCEPTION 'C3F2 injected commit failure'; END IF;
    RETURN NEW; END $$;
    CREATE CONSTRAINT TRIGGER c3f2_fail_commit AFTER UPDATE ON "Cremaciones"
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION c3f2_fail_commit();`);
  try {
    const failure = await request(`cremations/${s.cases.atomic}`, 'PUT', payload('atomic'), token);
    assert.equal(failure.status, 500, 'Intentional database commit fault');
    assert.deepEqual(snapshot(), atomicBefore, 'Commit failure rolls back every write');
  } finally {
    sql('DROP TRIGGER c3f2_fail_commit ON "Cremaciones"; DROP FUNCTION c3f2_fail_commit();');
  }
  console.log('F2-11 PASS: injected commit failure rolls back quote/package/accessory; no account/history');
  const race = await Promise.all(['accessory', 'plain'].map(pack =>
    request(`cremations/${s.cases.concurrent}`, 'PUT', payload('concurrent', pack), token)));
  assert.deepEqual(race.map(r => r.status).sort(), [200,409]);
  assert.match(race.find(r => r.status === 409).data.message, /paquete|concurr|cambi|intente/i);
  const winner = race.find(r => r.status === 200).data;
  assert.equal(winner.includesPawPrint, winner.cremationPackageId === s.packages.accessory.id);
  assert.equal(winner.quotedPrice, winner.includesPawPrint ? 850 : 600);
  assert.equal(winner.accessoryDescription, winner.includesPawPrint ? 'Huella de recuerdo' : null);
  console.log('F2-12 PASS: concurrent quotes yield 200/409 and coherent winner');
  for (const name of ['inProgress','cooling','processing','ready','delivered']) {
    const value = await update(name, 'accessory', '  Descripción inicial  ');
    assert.equal(value.accessoryDescription, 'Descripción inicial');
    await reject(name, 'accessory', 'Cambio posterior');
  }
  await reject('cancelled');
  // Whitespace request uses the existing package default, including a null default.
  sql(`UPDATE "PaquetesCremacion" SET "DescripcionAccesorio"=NULL WHERE "Id"=${literal(s.packages.accessory.id)};`);
  const nullValue = await update('nullDefault', 'accessory', '   ');
  assert.equal(nullValue.accessoryDescription, null);
  assert.equal(nullValue.includesPawPrint, true);
  sql(`UPDATE "PaquetesCremacion" SET "DescripcionAccesorio"='Huella de recuerdo' WHERE "Id"=${literal(s.packages.accessory.id)};`);
  // No automatic PaymentAccount is part of UpdateAsync, even on the plain baseline.
  const quoted = snapshot();
  assert.deepEqual(quoted.CuentasPago, before.CuentasPago);
  for (const [name, pack] of [['accessory','accessory'], ['plain','plain']]) {
    const c = cremation(quoted, name); const p = s.packages[pack];
    assert.equal(c.PaqueteCremacionId, p.id);
    assert.equal(c.NombrePaquete, p.name);
    assert.equal(c.PrecioCotizado, p.amount);
    assert.equal(c.PesoCotizadoKg, 5);
    assert.equal(c.PesoMinimoCotizadoKg, 0);
    assert.equal(c.PesoMaximoCotizadoKg, 5);
    assert.equal(c.IncluyeCertificado, true);
    await api('payments/accounts', 'POST', { cremationId: c.Id }, token, 201);
  }
  const accountRace = await Promise.all([1,2].map(() =>
    request('payments/accounts', 'POST', { cremationId: s.cases.concurrent }, token)));
  assert.deepEqual(accountRace.map(r => r.status).sort(), [201,409]);
  const after = snapshot();
  for (const [name, pack] of [['accessory','accessory'], ['plain','plain']]) {
    const accounts = after.CuentasPago.filter(p => p.CremacionId === s.cases[name]);
    assert.equal(accounts.length, 1);
    const p = accounts[0];
    assert.equal(p.PaqueteCremacionId, s.packages[pack].id);
    assert.equal(p.NombrePaqueteSnapshot, s.packages[pack].name);
    assert.equal(p.TotalServicio, s.packages[pack].amount);
    for (const field of ['PrecioCremacionId','TipoCremacionSnapshot','PesoBaseSnapshotKg','PesoMinimoSnapshotKg','PesoMaximoSnapshotKg'])
      assert.equal(p[field], null, `${field}: preserve existing separate account API behavior`);
    assert.equal(p.RequiereRevisionFinanciera, false);
    assert.equal(p.FechaResolucionRevisionFinanciera, null);
  }
  assert.equal(after.CuentasPago.filter(p => p.CremacionId === s.cases.concurrent).length, 1);
  assert.deepEqual(after.Pagos, before.Pagos);
  assert.deepEqual(after.Recolecciones, before.Recolecciones);
  for (const table of histories.filter(t => t !== 'CambiosAccesorioCremacion'))
    assert.deepEqual(after[table], before[table], `${table}: historical evidence preserved`);
  assert.deepEqual(after.CambiosAccesorioCremacion.filter(x => x.CremacionId === s.cases.accessory), amendment);
  assert.deepEqual(after.CambiosAccesorioCremacion.filter(x => x.CremacionId !== s.cases.accessory), before.CambiosAccesorioCremacion);
  for (const name of Object.keys(s.cases)) {
    for (const field of ['Estado','FechaInicio','FechaFinalizacion','FechaListaParaEntrega','FechaEntrega',
      'FechaProgramada','MontoPagoRequeridoInicio','InstruccionesEspeciales','Notas','AsignadoAUsuarioId'])
      assert.deepEqual(cremation(after,name)[field], cremation(before,name)[field], `${name}.${field}`);
  }
  const ordinary = await login(s.actors.ordinary);
  const authBefore = snapshot();
  await api(`cremations/${s.cases.invalid}`, 'PUT', payload('invalid'), ordinary, 403);
  assert.deepEqual(snapshot(), authBefore);
  console.log('F2-10 PASS: separate account API, SQL commercial evidence, unique accounts, no invented payments/reconciliation');
  console.log('PASS: lifecycle, normalization, authorization and historical evidence regression checks');
}
