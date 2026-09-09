// Renders the portfolio's /Cv page to a static A4 PDF.
//
//   node generate.mjs                      publish, serve, render, write the PDF
//   node generate.mjs --url http://...     render a URL that is already running
//   node generate.mjs --out ../../foo.pdf  write somewhere else
//   node generate.mjs --png shot.png       also save an A4 preview image
//   node generate.mjs --site https://...   public origin for in-site links
//
// The PDF is produced through the print stylesheet, so what you get here is
// exactly what Ctrl+P on the page gives — Pages/Cv.razor.css owns the A4 layout.

import { chromium } from 'playwright';
import { createServer } from 'node:http';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, rmSync, statSync, readFileSync } from 'node:fs';
import { readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(HERE, '..', '..');
const DEFAULT_OUT = path.join(REPO, 'wwwroot', 'cv-frederic-larue.pdf');

// The page uses root-relative links so the live site keeps SPA routing. A PDF
// has no origin to resolve those against, so Chromium would bake in whichever
// host rendered it — i.e. the throwaway localhost server below. They are
// rewritten to this origin just before the PDF is written.
const DEFAULT_SITE = 'https://fredericlarue.com';

const MIME = {
    '.html': 'text/html; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.js': 'text/javascript; charset=utf-8',
    '.mjs': 'text/javascript; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.wasm': 'application/wasm',
    '.dll': 'application/octet-stream',
    '.pdb': 'application/octet-stream',
    '.dat': 'application/octet-stream',
    '.blat': 'application/octet-stream',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg',
    '.gif': 'image/gif',
    '.svg': 'image/svg+xml',
    '.ico': 'image/x-icon',
    '.webp': 'image/webp',
    '.woff': 'font/woff',
    '.woff2': 'font/woff2',
    '.mp4': 'video/mp4',
};

function parseArgs(argv) {
    const args = { out: DEFAULT_OUT, url: null, png: null, site: DEFAULT_SITE };
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === '--out') args.out = path.resolve(argv[++i]);
        else if (argv[i] === '--png') args.png = path.resolve(argv[++i]);
        else if (argv[i] === '--site') args.site = argv[++i].replace(/\/$/, '');
        else if (argv[i] === '--url') args.url = argv[++i];
        else if (argv[i] === '--help' || argv[i] === '-h') args.help = true;
        else throw new Error(`unknown argument: ${argv[i]}`);
    }
    return args;
}

function publish() {
    const out = mkdtempSync(path.join(tmpdir(), 'cv-pdf-'));
    console.log('Publishing the site (Release)...');
    execFileSync(
        'dotnet',
        ['publish', path.join(REPO, 'Portfolio.csproj'), '-c', 'Release', '-o', out],
        { stdio: 'inherit' },
    );
    return out;
}

// Static file server with the SPA fallback the deployed site gets from
// wwwroot/staticwebapp.config.json — without it, /Cv is a 404 here.
function serve(root) {
    const server = createServer(async (req, res) => {
        const requested = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
        let file = path.join(root, requested);

        // Keep traversal inside the served directory.
        if (!file.startsWith(root)) {
            res.writeHead(403).end();
            return;
        }

        try {
            if (statSync(file).isDirectory()) file = path.join(file, 'index.html');
        } catch {
            file = path.join(root, 'index.html');
        }

        try {
            const body = await readFile(file);
            res.writeHead(200, { 'content-type': MIME[path.extname(file).toLowerCase()] ?? 'application/octet-stream' });
            res.end(body);
        } catch {
            res.writeHead(404).end();
        }
    });

    return new Promise((resolve) => {
        server.listen(0, '127.0.0.1', () => resolve({ server, port: server.address().port }));
    });
}

async function render(url, out, png, site) {
    const browser = await chromium.launch();
    try {
        // A4 at 96dpi, so the --png preview frames the same box as the PDF.
        const page = await browser.newPage({ viewport: { width: 794, height: 1123 } });

        const failures = [];
        page.on('requestfailed', (r) => failures.push(r.url()));
        page.on('pageerror', (e) => failures.push(`page error: ${e.message}`));

        // Lay the page out in print mode before measuring anything.
        await page.emulateMedia({ media: 'print' });

        console.log(`Rendering ${url} ...`);
        await page.goto(url, { waitUntil: 'networkidle', timeout: 120_000 });

        // networkidle fires once index.html has landed, which is well before
        // WebAssembly has booted and rendered the CV. Wait for the real thing.
        await page.waitForSelector('.cv-layout', { timeout: 120_000 });

        // Then wait for the webfont and the photo, or they miss the snapshot.
        await page.evaluate(() => document.fonts.ready);
        await page.evaluate(() => Promise.all(
            [...document.images]
                .filter((img) => !img.complete)
                .map((img) => new Promise((done) => { img.onload = img.onerror = done; })),
        ));

        // Absolutise in-site links, or the PDF ships pointing at localhost.
        const rewritten = await page.evaluate((origin) => {
            const out = [];
            for (const a of document.querySelectorAll('a[href^="/"]')) {
                const href = a.getAttribute('href');
                if (href.startsWith('//')) continue;
                a.setAttribute('href', origin + href);
                out.push(origin + href);
            }
            return out;
        }, site);

        if (rewritten.length) {
            console.log(`Rewrote ${rewritten.length} in-site link(s) to ${site}`);
        }

        await page.pdf({
            path: out,
            format: 'A4',
            printBackground: true,
            margin: { top: '0', right: '0', bottom: '0', left: '0' },
        });

        if (png) {
            await page.screenshot({ path: png, fullPage: true });
            console.log(`Wrote ${png}`);
        }

        if (failures.length) {
            console.warn(`\nWarning: ${failures.length} request(s) failed while rendering:`);
            for (const f of failures.slice(0, 10)) console.warn(`  ${f}`);
        }
    } finally {
        await browser.close();
    }
}

async function main() {
    const args = parseArgs(process.argv.slice(2));
    if (args.help) {
        console.log(readFileSync(fileURLToPath(import.meta.url), 'utf8').split('\n').slice(0, 9).join('\n'));
        return;
    }

    let published = null;
    let server = null;

    try {
        let url = args.url;
        if (!url) {
            published = publish();
            const started = await serve(path.join(published, 'wwwroot'));
            server = started.server;
            url = `http://127.0.0.1:${started.port}/Cv`;
        }

        await render(url, args.out, args.png, args.site);
    } finally {
        if (server) server.close();
        if (published) rmSync(published, { recursive: true, force: true });
    }

    const bytes = statSync(args.out).size;
    // Rough, but enough to catch the CV spilling onto a second page.
    const pages = (readFileSync(args.out).toString('latin1').match(/\/Type\s*\/Page[^s]/g) ?? []).length;

    console.log(`\nWrote ${args.out}`);
    console.log(`  ${(bytes / 1024).toFixed(0)} KB, ~${pages} page(s)`);
    if (pages > 1) console.log('  Note: a CV that spills past one page usually means the print type scale in Pages/Cv.razor.css needs trimming.');
}

main().catch((err) => {
    console.error(err.message ?? err);
    process.exit(1);
});
