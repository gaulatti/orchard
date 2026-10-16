const fs = require('node:fs/promises'), path = require('node:path'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const option = name => { const at = process.argv.indexOf(name); assert.ok(at >= 0 && process.argv[at + 1], `Pass ${name} <path>.`); return path.resolve(process.argv[at + 1]); };
const output = option('--output'), esbuildPath = option('--esbuild-module'), playwrightPath = option('--playwright-module');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
(async () => {
    await fs.mkdir(output, {recursive: true});
    const referenceBytes = await fs.readFile('tests/Reference/DataList.json'), reference = JSON.parse(referenceBytes);
    const source = path.resolve('artifacts/line-chart-browser/frozen-source/src/components/data-list.tsx');
    assert.equal(hash(await fs.readFile(source)), reference.sourceSha256);
    const runtime = path.resolve('artifacts/line-chart-browser/node_modules');
    for (const name of ['react', 'react-dom']) assert.equal(require(path.join(runtime, name, 'package.json')).version, reference.react);
    assert.equal(require(path.join(esbuildPath, 'package.json')).version, reference.esbuild);
    assert.equal(require(path.join(playwrightPath, 'package.json')).version, reference.playwright);
    const bundle = await require(esbuildPath).build({stdin: {contents: `import React from'react';import{createRoot}from'react-dom/client';import{flushSync}from'react-dom';import{DataList}from ${JSON.stringify(source)};const root=createRoot(document.getElementById('root'));const node=v=>v&&typeof v==='object'&&v.action?React.createElement('button',null,v.action):v;window.show=p=>flushSync(()=>root.render(<DataList {...p} items={p.items.map(i=>({...i,label:node(i.label),value:node(i.value),description:node(i.description)}))}/>));`, loader: 'tsx', resolveDir: process.cwd()}, bundle: true, write: false, platform: 'browser', nodePaths: [runtime], define: {'process.env.NODE_ENV': '"production"'}});
    const cssBytes = await fs.readFile('artifacts/line-chart-browser/styles.css'); assert.equal(hash(cssBytes), reference.cssSha256);
    let fonts = '';
    for (const font of reference.fontFiles) {
        const bytes = await fs.readFile('src/Orchard/Assets/Fonts/' + font.file); assert.equal(hash(bytes), font.sha256);
        fonts += `@font-face{font-family:'${font.family}';font-weight:${font.weight};src:url(data:font/ttf;base64,${bytes.toString('base64')})}`;
    }
    const browser = await require(playwrightPath).chromium.launch({headless: true, channel: 'msedge'});
    assert.equal(browser.version(), reference.browser);
    const reports = [];
    try {
        for (const test of reference.cases) {
            const page = await browser.newPage({viewport: {width: test.viewportWidth, height: 900}, deviceScaleFactor: 1, reducedMotion: 'reduce'});
            try {
                await page.setContent(`<html class="${test.scheme === 'Dark' ? 'dark' : ''}"><head><style>${cssBytes.toString().replace(/@import url\([^\r\n]*\);/g, '')}${fonts}body{padding:20px;margin:0}#root{width:${test.width}px;direction:${test.direction}}</style></head><body><div id="root"></div><script>${bundle.outputFiles[0].text}</script></body></html>`);
                await page.evaluate(p => window.show(p), test.props);
                await page.evaluate(async () => { await Promise.all([document.fonts.load('500 13px "Encode Sans"'), document.fonts.load('500 14px "Encode Sans"'), document.fonts.load('400 12px "Libre Franklin"')]); await document.fonts.ready; });
                const rows = await page.locator('#root').evaluate(root => {
                    const origin = root.getBoundingClientRect();
                    const text = element => {
                        if (!element) return null;
                        const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT, {acceptNode: node => node.parentElement.closest('p') && node.parentElement.closest('p') !== element ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT});
                        const nodes = []; while (walker.nextNode()) nodes.push(walker.currentNode);
                        const content = nodes.map(node => node.textContent).join('');
                        const first = nodes.map(node => ({node, index: node.textContent.search(/[^ \t\r\n\f]/)})).find(value => value.index >= 0);
                        if (!first) return {text: content, firstBaseline: null};
                        const {node, index} = first, character = String.fromCodePoint(node.textContent.codePointAt(index));
                        const range = document.createRange(); range.setStart(node, index); range.setEnd(node, index + character.length);
                        const rect = range.getBoundingClientRect(), style = getComputedStyle(node.parentElement);
                        const context = document.createElement('canvas').getContext('2d'); context.font = style.font;
                        const metrics = context.measureText(character);
                        if (Math.abs(rect.height - metrics.fontBoundingBoxAscent - metrics.fontBoundingBoxDescent) > .001) throw Error('DOM range and measured font line boxes disagree.');
                        return {text: content, firstBaseline: rect.y - origin.y + metrics.fontBoundingBoxAscent, lineHeight: style.lineHeight,
                            rangeAscent: metrics.fontBoundingBoxAscent, rangeDescent: metrics.fontBoundingBoxDescent, font: style.font, rich: node.parentElement !== element};
                    };
                    return [...root.firstElementChild.children].map(row => ({label: text(row.querySelector('dt')), value: text(row.querySelector('dd')), description: text(row.querySelector('p'))}));
                });
                reports.push({id: test.id, viewportWidth: test.viewportWidth, scheme: test.scheme, rows});
            } finally { await page.close(); }
        }
    } finally { await browser.close(); }
    const report = {format: 'orchard-data-list-typography-reference-v1', sourceCommit: reference.sourceCommit, sourceSha256: reference.sourceSha256,
        normalReferenceSha256: hash(referenceBytes), scriptSha256: hash((await fs.readFile(__filename, 'utf8')).replace(/\r\n/g, '\n')), scriptNormalization: 'lf',
        browser: reference.browser, fontFiles: reference.fontFiles, reports};
    await fs.writeFile(path.join(output, 'reference.json'), JSON.stringify(report, null, 2) + '\n');
    console.log(JSON.stringify({cases: reports.length, baselines: reports.flatMap(test => test.rows.flatMap(row => Object.values(row))).filter(text => text?.firstBaseline != null).length, first: reports[0]}));
})().catch(error => { console.error(error); process.exitCode = 1; });
