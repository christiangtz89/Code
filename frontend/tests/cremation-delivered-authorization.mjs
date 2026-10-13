// Run with Node 24: node tests/cremation-delivered-authorization.mjs
// Exercises the real TypeScript visibility functions without a browser or test framework.
import assert from 'node:assert/strict';
import { registerHooks } from 'node:module';
import { readFileSync } from 'node:fs';
import ts from 'typescript';

registerHooks({
  resolve(specifier, context, nextResolve) {
    try { return nextResolve(specifier, context); }
    catch (error) {
      if (error.code !== 'ERR_MODULE_NOT_FOUND' || !specifier.startsWith('.')) throw error;
      return nextResolve(`${specifier}.ts`, context);
    }
  },
  load(url, context, nextLoad) {
    if (!url.endsWith('.ts')) return nextLoad(url, context);
    return { format: 'module', shortCircuit: true, source: ts.transpileModule(
      readFileSync(new URL(url), 'utf8'),
      { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } },
    ).outputText };
  },
});
const { canAmendCremationInstructions } = await import('../src/features/cremations/utils/cremationInstructions.ts');
const { canAmendCremationAccessory } = await import('../src/features/cremations/utils/cremationAccessory.ts');
let token;
globalThis.localStorage = { getItem: () => token };
const setClaims = claims => {
  token = `${Buffer.from('{}').toString('base64url')}.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.test`;
};
const check = (claims, status, expected) => {
  setClaims(claims);
  const cremation = { status, isActive: true, includesPawPrint: true };
  for (const canAmend of [canAmendCremationInstructions, canAmendCremationAccessory]) {
    assert.equal(canAmend(cremation), expected, `${canAmend.name}: ${JSON.stringify(claims)}, status ${status}`);
    assert.equal(canAmend({ ...cremation, isActive: false }), false);
  }
  assert.equal(canAmendCremationAccessory({ ...cremation, includesPawPrint: false }), false);
};
const manage = { permission: ['Cremations.Manage'] };
for (const role of ['Owner', 'Admin', 'Protected Admin', 'Manager', 'Operator']) {
  check({ ...manage, role }, 8, false);
  for (const status of [3,4,5,6,7]) check({ ...manage, role }, status, true);
}
check({ pcms_owner: 'true' }, 8, true);
check({ ...manage, pcms_protected_admin: 'true', role: 'Admin' }, 8, true);
check({ pcms_protected_admin: 'true', role: 'Admin' }, 8, false);
check({ ...manage, pcms_protected_admin: 'false', role: 'Admin' }, 8, false);
check({ ...manage }, 8, false);
// Cancelled is an existing, separately authorized lifecycle and remains unchanged.
check({ pcms_owner: 'true' }, 9, true);
check({ ...manage, role: 'Admin' }, 9, true);
check({ ...manage, role: 'Manager' }, 9, false);
check({ ...manage, role: 'Owner' }, 9, false);
token = null;
assert.equal(canAmendCremationInstructions({ status: 8, isActive: true }), false);
console.log('PASS Delivered visibility uses authoritative identity claims; active/Cancelled visibility preserved');
