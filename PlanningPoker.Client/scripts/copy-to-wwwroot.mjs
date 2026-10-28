// Copies the Angular production build into the API's wwwroot so the API serves the SPA.
import { cpSync, rmSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const source = fileURLToPath(new URL('../dist/PlanningPoker.Client/browser', import.meta.url));
const target = fileURLToPath(new URL('../../PlanningPoker.Api/wwwroot', import.meta.url));

rmSync(target, { recursive: true, force: true });
mkdirSync(target, { recursive: true });
cpSync(source, target, { recursive: true });
console.log(`Copied ${source} -> ${target}`);
