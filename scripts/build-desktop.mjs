import { execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const rootDir = process.cwd();
const frontendDir = path.join(rootDir, 'cliente', 'pos-ui');
const frontendDistDir = path.join(frontendDir, 'dist');
const backendProjectDir = path.join(rootDir, 'servidor', 'src', 'ApiWeb');
const backendWwwrootDir = path.join(backendProjectDir, 'wwwroot');
const backendPublishDir = path.join(rootDir, 'desktop-build', 'backend');

function run(command, cwd, extraEnv = {}) {
  execSync(command, {
    cwd,
    stdio: 'inherit',
    env: {
      ...process.env,
      ...extraEnv
    }
  });
}

function resetDirectory(targetDir) {
  fs.rmSync(targetDir, { recursive: true, force: true });
  fs.mkdirSync(targetDir, { recursive: true });
}

resetDirectory(backendWwwrootDir);
resetDirectory(backendPublishDir);

run('npm ci', frontendDir);
run('npm run build', frontendDir, { VITE_API_BASE_URL: '' });

fs.cpSync(frontendDistDir, backendWwwrootDir, { recursive: true });

run(
  'dotnet publish ApiWeb.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false -o "' + backendPublishDir + '"',
  backendProjectDir
);
