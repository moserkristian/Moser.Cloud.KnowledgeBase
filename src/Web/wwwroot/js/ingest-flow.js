(function () {
    "use strict";

    const NS = "http://www.w3.org/2000/svg";
    const I = {
        disk: '<path d="M4 6h16v12H4z"/><path d="M4 10h16M8 14h4"/>',
        synth: '<path d="M12 3v4M8 7h8"/><rect x="5" y="11" width="14" height="10" rx="2"/><path d="M9 15h6M9 18h4"/>',
        chunk: '<path d="M5 5h6v6H5zM13 5h6v6h-6zM5 13h6v6H5zM13 13h6v6h-6z"/>',
        embed: '<circle cx="7" cy="8" r="2"/><circle cx="17" cy="7" r="2"/><circle cx="12" cy="16" r="2.2"/><path d="M8.7 9.2 11 14.2M15.4 8.6l-2.2 5.4"/>',
        vs: '<ellipse cx="12" cy="6" rx="7" ry="3"/><path d="M5 6v12c0 1.7 3.1 3 7 3s7-1.3 7-3V6"/><path d="M5 12c0 1.7 3.1 3 7 3s7-1.3 7-3"/>',
        all: '<path d="M4 7h16v12H4z"/><path d="M8 11h8M8 15h6"/>'
    };
    const ico = (k) =>
        `<svg class="ico" viewBox="0 0 24 24" fill="none" stroke="url(#igAccent)" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${I[k]}</svg>`;
    const capIco = (k) =>
        `<svg viewBox="0 0 24 24" fill="none" stroke="url(#igAccent)" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">${I[k]}</svg>`;

    const HERO = "expense-receipts.md";
    const HERO_TOKENS = 148;
    const HERO_DOC =
        "Expense policy — receipts (FIN-EXP-25)\n\nItemized receipts are required for any expense over $25.\nCard statements are not a substitute.";

    const POLICIES = [
        { name: "annual-leave-eu.md", tokens: 121 },
        { name: "code-of-conduct.md", tokens: 156 },
        { name: "customer-support-sla.md", tokens: 134 },
        { name: "data-classification.md", tokens: 142 },
        { name: "expense-field-sales.md", tokens: 138 },
        { name: HERO, tokens: HERO_TOKENS, hero: true },
        { name: "gift-and-hospitality.md", tokens: 167 },
        { name: "information-security.md", tokens: 159 },
        { name: "insider-trading.md", tokens: 128 },
        { name: "password-policy.md", tokens: 118 },
        { name: "procurement-approval.md", tokens: 140 },
        { name: "pto-us.md", tokens: 126 },
        { name: "refund-premium.md", tokens: 132 },
        { name: "refund-standard.md", tokens: 129 },
        { name: "remote-work-policy.md", tokens: 135 },
        { name: "returns-policy.md", tokens: 124 },
        { name: "travel-policy.md", tokens: 147 },
        { name: "vendor-offboarding.md", tokens: 119 },
        { name: "vendor-onboarding.md", tokens: 131 }
    ];

    const NODES = {
        disk: { col: 0, ico: "disk", title: "Seed files", sub: "data/seed/policy", badge: "19 .md", files: true },
        synth: { col: 0, ico: "synth", title: "FAQ synth", sub: "later · 50 in-proc", badge: "not this file" },
        chunker: { col: 1, ico: "chunk", title: "Window", sub: "max 640 · overlap 96", badge: "measure" },
        embed: { col: 2, ico: "embed", title: "Coordinates", sub: "nomic-embed-text", badge: "768-d" },
        vs: { col: 3, ico: "vs", title: "Vector store", sub: "policy-chunks", badge: "search" },
        all: { col: 3, ico: "all", title: "Text list", sub: "_all hybrid", badge: "words" }
    };
    const ORDER = { 0: ["disk", "synth"], 1: ["chunker"], 2: ["embed"], 3: ["vs", "all"] };
    const EDGES = [
        ["disk", "chunker", "read"],
        ["synth", "chunker", ""],
        ["chunker", "embed", "keep text"],
        ["embed", "vs", "row"],
        ["embed", "all", "copy"]
    ];

    const BEATS = [
        {
            node: "disk",
            from: "disk",
            kick: "01  ·  file",
            title: "This is still just a markdown file",
            why: "Nothing is searchable yet. A chat model does not open your disk. Ingest has to make a row it can find later.",
            same: "The words you would read in an editor.",
            change: "We pick one real seed file and follow only that.",
            doc: true, meter: false, vec: false
        },
        {
            node: "chunker",
            from: "chunker",
            via: ["disk", "chunker"],
            kick: "02  ·  window",
            title: "Split only if it does not fit",
            why: "Search and prompts work on slices. The window here is 640 tokens. Overlap 96 is glue between slices — unused when nothing splits.",
            same: "The whole receipts policy still fits in one slice.",
            change: "148 / 640 → one chunk. No cut.",
            doc: true, meter: true, vec: false
        },
        {
            node: "embed",
            from: "embed",
            via: ["chunker", "embed"],
            kick: "03  ·  coordinates",
            title: "Meaning becomes a point — the text stays",
            why: "A vector is a location. Nearby points are similar ideas (receipts, expenses), not similar spelling. Search will compare locations. The model will never see these numbers.",
            same: "The same sentences are kept.",
            change: "A 768-number coordinate is added so we can find this row without rereading every file.",
            doc: true, meter: false, vec: true
        },
        {
            node: "vs",
            from: "vs",
            via: ["embed", "vs"],
            kick: "04  ·  shelf",
            title: "A row: text for the model, point for search",
            why: "The shelf is the index. A copy also goes to a plain list so word-overlap can help when vectors are close but the words do not match.",
            same: "Same chunk, now addressable.",
            change: "A labeled point appears on the map. That is this policy among others.",
            doc: true, meter: false, vec: true, store: true
        }
    ];

    const CLUSTER = {
        expense: [22, 74], gift: [78, 22], pto: [18, 22], refund: [76, 78],
        security: [50, 14], travel: [88, 50], vendor: [62, 86], conduct: [40, 44],
        remote: [10, 52], sla: [50, 58], other: [52, 50]
    };
    const CLUSTER_LAB = [
        { t: "expense", x: 22, y: 86 },
        { t: "gifts", x: 78, y: 12 },
        { t: "time off", x: 18, y: 12 }
    ];

    const EXCERPT = {
        "expense-receipts.md": "Itemized receipts are required for any expense over $25. Card statements are not a substitute.",
        "gift-and-hospitality.md": "Cash gifts are prohibited. Branded items under $25 are generally allowed.",
        "pto-us.md": "US PTO is tracked in Workday. Do not invent extra days.",
        "annual-leave-eu.md": "EU annual leave follows the local HRIS and statutory minimums.",
        "refund-standard.md": "Standard refunds follow the posted window in the matching policy.",
        "information-security.md": "Report suspected phishing to infosec within one hour.",
        "expense-field-sales.md": "Field sales expenses still need receipts over $25."
    };

    const PROBES = {
        expense: { q: "Need a receipt over $25?", topic: "expense" },
        gift: { q: "Cash gift from a supplier?", topic: "gift" },
        pto: { q: "How many PTO days?", topic: "pto" }
    };

    let ctl = null;

    function esc(s) {
        return String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
    }
    function hash32(s) {
        let h = 2166136261;
        for (let i = 0; i < s.length; i++) h = Math.imul(h ^ s.charCodeAt(i), 16777619);
        return h >>> 0;
    }
    function vecPreview(name) {
        const h = hash32(name);
        const n = (s) => (((h >>> s) & 255) / 127.5 - 1).toFixed(2);
        return `[${n(0)}, ${n(8)}, ${n(16)}, …]`;
    }
    function topicOf(name) {
        const n = name.toLowerCase();
        if (n.includes("expense") || n.includes("receipt")) return "expense";
        if (n.includes("gift")) return "gift";
        if (n.includes("pto") || n.includes("leave") || n.includes("annual")) return "pto";
        if (n.includes("refund") || n.includes("return")) return "refund";
        if (n.includes("password") || n.includes("security") || n.includes("classif")) return "security";
        if (n.includes("travel")) return "travel";
        if (n.includes("vendor")) return "vendor";
        if (n.includes("conduct") || n.includes("insider")) return "conduct";
        if (n.includes("remote")) return "remote";
        if (n.includes("sla") || n.includes("support")) return "sla";
        return "other";
    }
    function excerptOf(name) {
        return EXCERPT[name] || "Policy chunk stored with its embedding.";
    }
    function place(name) {
        const topic = topicOf(name);
        const c = CLUSTER[topic] || CLUSTER.other;
        if (name === HERO) return { topic, x: c[0], y: c[1] };
        const h = hash32(name);
        return {
            topic,
            x: Math.max(8, Math.min(92, c[0] + ((h % 13) - 6) * 1.2)),
            y: Math.max(10, Math.min(90, c[1] + (((h >> 5) % 13) - 6) * 1.1))
        };
    }
    function capContent(from, d) {
        switch (from) {
            case "disk":
                return { cls: "PolicyDocument", ico: "disk", fields: [["file", d.file], ["tokens", String(d.tokens)]] };
            case "chunker":
                return { cls: "PolicyChunk", ico: "chunk", fields: [["id", d.id], ["window", d.tokens + " / 640"], ["split", "none"]], meter: 23 };
            case "embed":
                return { cls: "EmbeddedChunk", ico: "embed", fields: [["text", "kept"], ["vec", d.preview]] };
            case "vs":
                return { cls: "IndexRow", ico: "vs", fields: [["text", "for the model"], ["point", "for search"]] };
            default:
                return { cls: "Message", ico: "disk", fields: [] };
        }
    }

    function start(root) {
        const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        const colsEl = root.querySelector("[data-ig=cols]");
        const graph = root.querySelector("[data-ig=graph]");
        const svg = root.querySelector("[data-ig=edges]");
        const flow = root.querySelector("[data-ig=flow]");
        const railEl = root.querySelector("[data-ig=rail]");
        const nextBtn = root.querySelector("[data-ig=next]");
        const backBtn = root.querySelector("[data-ig=back]");
        const playBtn = root.querySelector("[data-ig=play]");
        const burstBtn = root.querySelector("[data-ig=burst]");
        const tableEl = root.querySelector("[data-ig=table]");
        const spaceEl = root.querySelector("[data-ig=space]");
        const groundBody = root.querySelector("[data-ig=groundBody]");
        const teach = {
            kick: root.querySelector("[data-ig=teachKick]"),
            title: root.querySelector("[data-ig=teachTitle]"),
            why: root.querySelector("[data-ig=teachWhy]"),
            same: root.querySelector("[data-ig=teachSame]"),
            change: root.querySelector("[data-ig=teachChange]"),
            doc: root.querySelector("[data-ig=teachDoc]"),
            meter: root.querySelector("[data-ig=teachMeter]"),
            meterLbl: root.querySelector("[data-ig=teachMeterLbl]"),
            vec: root.querySelector("[data-ig=teachVec]")
        };

        if (!colsEl || !graph || !svg || !flow) return { destroy() {} };

        const defs = document.createElementNS(NS, "defs");
        const grad = document.createElementNS(NS, "linearGradient");
        grad.id = "igAccent";
        grad.setAttribute("x1", "0"); grad.setAttribute("y1", "0");
        grad.setAttribute("x2", "1"); grad.setAttribute("y2", "1");
        grad.innerHTML = '<stop offset="0" stop-color="#8b929f"/><stop offset=".5" stop-color="#f6f8fc"/><stop offset="1" stop-color="#e8ecf3"/>';
        defs.appendChild(grad);
        svg.appendChild(defs);

        const nodeEls = {};
        colsEl.replaceChildren();
        for (let c = 0; c < 4; c++) {
            const col = document.createElement("div");
            col.className = "ig-col";
            (ORDER[c] || []).forEach((id) => {
                const n = NODES[id];
                const el = document.createElement("div");
                el.className = "ig-node" + (id === "synth" ? " dim" : "");
                el.dataset.id = id;
                el.innerHTML =
                    `${n.badge ? `<span class="n-badge">${n.badge}</span>` : ""}${ico(n.ico)}` +
                    `<div class="n-title">${n.title}</div><div class="n-sub">${n.sub}</div>` +
                    (n.files ? `<div class="n-files" data-ig="files"></div>` : "");
                col.appendChild(el);
                nodeEls[id] = el;
            });
            colsEl.appendChild(col);
        }

        const filesBox = root.querySelector("[data-ig=files]");
        function showFiles(active) {
            if (!filesBox) return;
            const i = Math.max(0, POLICIES.findIndex((f) => f.name === active));
            const start = Math.max(0, Math.min(i - 2, POLICIES.length - 6));
            filesBox.innerHTML = POLICIES.slice(start, start + 6)
                .map((f) => `<div class="${f.name === active ? "on" : ""}">${f.name}</div>`)
                .join("");
        }
        showFiles(HERO);

        const pathMap = {};
        const edgeKey = (a, b) => a + ">" + b;
        function center(id) {
            const r = nodeEls[id].getBoundingClientRect();
            const g = graph.getBoundingClientRect();
            return {
                l: r.left - g.left, r: r.right - g.left,
                t: r.top - g.top, b: r.bottom - g.top,
                cx: r.left - g.left + r.width / 2,
                cy: r.top - g.top + r.height / 2
            };
        }
        function edgeD(a, b) {
            const A = center(a), B = center(b);
            const dx = B.cx - A.cx, dy = B.cy - A.cy;
            let x1, y1, x2, y2, c1x, c1y, c2x, c2y;
            if (Math.abs(dx) >= Math.abs(dy)) {
                if (dx >= 0) { x1 = A.r; x2 = B.l; } else { x1 = A.l; x2 = B.r; }
                y1 = A.cy; y2 = B.cy;
                const m = (x1 + x2) / 2; c1x = m; c1y = y1; c2x = m; c2y = y2;
            } else {
                if (dy >= 0) { y1 = A.b; y2 = B.t; } else { y1 = A.t; y2 = B.b; }
                x1 = A.cx; x2 = B.cx;
                const m = (y1 + y2) / 2; c1x = x1; c1y = m; c2x = x2; c2y = m;
            }
            return `M${x1} ${y1} C ${c1x} ${c1y} ${c2x} ${c2y} ${x2} ${y2}`;
        }
        function buildEdges() {
            const w = graph.clientWidth, h = graph.clientHeight;
            svg.setAttribute("width", w);
            svg.setAttribute("height", h);
            [...svg.querySelectorAll(":scope > path, :scope > text")].forEach((n) => n.remove());
            const showLabels = w > 760;
            EDGES.forEach(([a, b, label]) => {
                const p = document.createElementNS(NS, "path");
                p.setAttribute("d", edgeD(a, b));
                p.setAttribute("fill", "none");
                p.setAttribute("stroke", label ? "var(--ig-live)" : "var(--ig-edge)");
                p.setAttribute("stroke-width", label ? "1.6" : "1");
                p.setAttribute("stroke-dasharray", label ? "2 8" : "2 6");
                p.setAttribute("class", label ? "ig-flowpath" : "");
                p.setAttribute("opacity", label ? "1" : "0.35");
                svg.appendChild(p);
                pathMap[edgeKey(a, b)] = p;
                if (showLabels && label) {
                    try {
                        const len = p.getTotalLength(), pt = p.getPointAtLength(len * 0.5);
                        const t = document.createElementNS(NS, "text");
                        t.setAttribute("x", String(pt.x));
                        t.setAttribute("y", String(pt.y - 6));
                        t.setAttribute("text-anchor", "middle");
                        t.setAttribute("font-family", "JetBrains Mono, ui-monospace, monospace");
                        t.setAttribute("font-size", "9");
                        t.setAttribute("fill", "#d3d8e0");
                        t.setAttribute("stroke", "#050506");
                        t.setAttribute("stroke-width", "3");
                        t.setAttribute("paint-order", "stroke");
                        t.textContent = label;
                        svg.appendChild(t);
                    } catch (_) { /* skip */ }
                }
            });
        }

        const store = [];
        let selectedId = null;
        let probeHits = [];
        let queryPt = null;
        let beat = 0;
        let auto = false;
        let moving = false;
        let moveT = 0;
        let movePath = null;
        let dead = false;
        let raf = 0;
        let last = performance.now();
        let dwell = 0;
        const heroD = {
            file: HERO,
            tokens: HERO_TOKENS,
            id: HERO + "#0",
            preview: vecPreview(HERO),
            excerpt: excerptOf(HERO)
        };

        const capEl = document.createElement("div");
        capEl.className = "ig-cap";
        flow.appendChild(capEl);
        let capH = 48;
        function renderCap(from) {
            const cc = capContent(from, heroD);
            capEl.className = "ig-cap";
            capEl.innerHTML =
                `<div class="cap-cls">${capIco(cc.ico)}<span>${cc.cls}</span></div>` +
                `<div class="cap-fields">${cc.fields.map((f) => `<span class="kv"><i>${f[0]}</i><b>${esc(f[1])}</b></span>`).join("")}</div>` +
                (cc.meter != null ? `<div class="cap-meter"><i style="width:${cc.meter}%"></i></div>` : "");
            capH = capEl.offsetHeight || 48;
        }
        function park(id) {
            const c = center(id);
            capEl.style.transform = `translate(${(c.cx - 24) | 0}px, ${(c.cy - capH / 2) | 0}px)`;
            Object.values(nodeEls).forEach((el) => el.classList.remove("lit"));
            nodeEls[id]?.classList.add("lit");
        }

        function renderRail() {
            if (!railEl) return;
            railEl.innerHTML = BEATS.map((b, i) =>
                `<li class="${i === beat ? "cur" : ""}${i < beat ? " done" : ""}"><span class="num">${String(i + 1).padStart(2, "0")}</span><span class="t">${b.kick.split("·")[1].trim()}</span></li>`
            ).join("");
        }
        function renderTeach() {
            const b = BEATS[beat];
            if (teach.kick) teach.kick.textContent = b.kick;
            if (teach.title) teach.title.textContent = b.title;
            if (teach.why) teach.why.textContent = b.why;
            if (teach.same) teach.same.textContent = b.same;
            if (teach.change) teach.change.textContent = b.change;
            if (teach.doc) {
                teach.doc.hidden = !b.doc;
                teach.doc.textContent = HERO_DOC;
            }
            if (teach.meter) {
                teach.meter.hidden = !b.meter;
                if (b.meter) {
                    teach.meter.querySelector("i").style.width = "23%";
                    if (teach.meterLbl) teach.meterLbl.textContent = "148 / 640 tokens · one chunk";
                }
            }
            if (teach.vec) {
                teach.vec.hidden = !b.vec;
                if (b.vec) {
                    const hs = [42, 78, 31, 64, 22, 88, 55, 40, 71, 18, 60, 47];
                    teach.vec.innerHTML = hs.map((h) => `<i style="height:${h}%"></i>`).join("") +
                        `<code>${esc(heroD.preview)} 768-d</code>`;
                }
            }
            if (nextBtn) {
                nextBtn.disabled = moving || beat >= BEATS.length - 1;
                nextBtn.textContent = beat >= BEATS.length - 1 ? "Done" : "Next";
            }
            if (backBtn) backBtn.disabled = moving || beat === 0;
        }

        function upsertIndex(d) {
            if (store.some((r) => r.id === d.id)) return;
            const pos = place(d.file);
            store.unshift({
                id: d.id, file: d.file, tokens: d.tokens, preview: d.preview,
                excerpt: d.excerpt || excerptOf(d.file), topic: pos.topic, x: pos.x, y: pos.y
            });
            renderTable();
            renderSpace();
        }
        function renderTable() {
            if (!tableEl) return;
            tableEl.innerHTML = store.map((r) => {
                const on = r.id === selectedId ? " on" : "";
                const hit = probeHits.includes(r.id) ? " hit" : "";
                return `<tr class="${on}${hit}" data-id="${esc(r.id)}"><td><code>${esc(r.id)}</code></td><td class="txt">${esc(r.excerpt)}</td><td class="vec">${esc(r.preview)}</td></tr>`;
            }).join("");
            tableEl.querySelectorAll("tr").forEach((tr) => {
                tr.addEventListener("click", () => selectRow(tr.getAttribute("data-id")));
            });
        }
        function selectRow(id) {
            selectedId = id;
            const row = store.find((r) => r.id === id);
            renderTable();
            renderSpace();
            if (row && groundBody) {
                groundBody.innerHTML = `<span class="pass"><b>${esc(row.id)}</b> — the model would read this text:<br>${esc(row.excerpt)}</span>`;
            }
        }
        function renderSpace() {
            if (!spaceEl) return;
            const labs = CLUSTER_LAB.map((l) =>
                `<text class="lab" x="${l.x}" y="${l.y}" text-anchor="middle">${l.t}</text>`
            ).join("");
            const dots = store.map((r) => {
                const cls = probeHits.includes(r.id) ? "pt hit" : (r.id === selectedId ? "pt on" : "pt");
                const rad = r.file === HERO ? 2.6 : 1.5;
                return `<circle class="${cls}" cx="${r.x}" cy="${r.y}" r="${rad}" data-id="${r.id}"></circle>`;
            }).join("");
            let extra = "";
            if (queryPt) {
                extra += probeHits.map((id) => {
                    const r = store.find((x) => x.id === id);
                    return r ? `<line class="ray" x1="${queryPt.x}" y1="${queryPt.y}" x2="${r.x}" y2="${r.y}"/>` : "";
                }).join("");
                extra += `<circle class="q" cx="${queryPt.x}" cy="${queryPt.y}" r="2.8"/>`;
            }
            spaceEl.innerHTML = labs + extra + dots;
            spaceEl.querySelectorAll("circle.pt").forEach((c) => {
                c.addEventListener("click", () => selectRow(c.getAttribute("data-id")));
            });
        }
        function runProbe(key) {
            const p = PROBES[key];
            if (!p) return;
            root.querySelectorAll("[data-probe]").forEach((b) => b.classList.toggle("on", b.getAttribute("data-probe") === key));
            if (store.length === 0) {
                if (groundBody) groundBody.textContent = "Walk the file to the shelf first (Next → …), then ask.";
                return;
            }
            const c = CLUSTER[p.topic] || CLUSTER.other;
            queryPt = { x: Math.min(92, c[0] + 6), y: Math.max(10, c[1] - 8) };
            const ranked = store
                .map((r) => ({ r, d: Math.hypot(r.x - queryPt.x, r.y - queryPt.y) + (r.topic === p.topic ? 0 : 12) }))
                .sort((a, b) => a.d - b.d);
            probeHits = ranked.slice(0, 3).map((x) => x.r.id);
            selectedId = probeHits[0];
            renderTable();
            renderSpace();
            if (groundBody) {
                const passes = ranked.slice(0, 3).map((x, i) =>
                    `<div class="pass">${i + 1}. <b>${esc(x.r.id)}</b> — ${esc(x.r.excerpt)}</div>`
                ).join("");
                groundBody.innerHTML =
                    `The question is a new point. Neighbors in the <b>${p.topic}</b> neighborhood. The model would read:<br>${passes}`;
            }
        }

        function applyBeat(i) {
            beat = i;
            const b = BEATS[beat];
            renderCap(b.from);
            park(b.node);
            renderRail();
            renderTeach();
            if (b.store) {
                upsertIndex(heroD);
                selectedId = heroD.id;
                nodeEls.vs?.classList.add("flash-ok");
                nodeEls.all?.classList.add("flash-ok");
                renderTable();
                renderSpace();
            }
        }

        function goTo(i) {
            if (moving || dead) return;
            i = Math.max(0, Math.min(BEATS.length - 1, i));
            if (i === beat) return;
            const dest = BEATS[i];
            if (i === beat + 1 && dest.via) {
                const path = pathMap[edgeKey(dest.via[0], dest.via[1])];
                if (path) {
                    moving = true;
                    moveT = 0;
                    movePath = path;
                    renderCap(BEATS[beat].from);
                    if (nextBtn) nextBtn.disabled = true;
                    return;
                }
            }
            applyBeat(i);
        }

        function finishMove() {
            moving = false;
            movePath = null;
            applyBeat(beat + 1);
            dwell = 0;
        }

        function loop(now) {
            if (dead) return;
            const dt = Math.min(0.05, (now - last) / 1000);
            last = now;
            if (moving && movePath) {
                let len = 1;
                try { len = movePath.getTotalLength() || 1; } catch (_) { len = 1; }
                moveT += (72 * dt) / len;
                if (moveT >= 1) {
                    finishMove();
                } else {
                    try {
                        const pt = movePath.getPointAtLength(moveT * len);
                        capEl.style.transform = `translate(${(pt.x + 8) | 0}px, ${(pt.y - capH / 2) | 0}px)`;
                    } catch (_) { /* skip */ }
                }
            } else if (auto && beat < BEATS.length - 1 && !moving) {
                dwell += dt;
                if (dwell > 2.6) {
                    dwell = 0;
                    goTo(beat + 1);
                }
            }
            raf = requestAnimationFrame(loop);
        }

        const ac = new AbortController();
        const sig = { signal: ac.signal };
        nextBtn?.addEventListener("click", () => { auto = false; if (playBtn) playBtn.textContent = "Play through"; goTo(beat + 1); }, sig);
        backBtn?.addEventListener("click", () => { auto = false; if (playBtn) playBtn.textContent = "Play through"; goTo(beat - 1); }, sig);
        playBtn?.addEventListener("click", () => {
            if (beat >= BEATS.length - 1) { applyBeat(0); auto = true; playBtn.textContent = "⏸ Pause"; dwell = 0; return; }
            auto = !auto;
            playBtn.textContent = auto ? "⏸ Pause" : "Play through";
            dwell = 0;
        }, sig);
        burstBtn?.addEventListener("click", () => {
            POLICIES.forEach((f) => upsertIndex({
                file: f.name, tokens: f.tokens, id: f.name + "#0",
                preview: vecPreview(f.name), excerpt: excerptOf(f.name)
            }));
            nodeEls.synth?.classList.remove("dim");
            if (groundBody && store.length > 1) {
                groundBody.textContent = "The map now holds the seed. Ask a question — it should land in the matching neighborhood.";
            }
        }, sig);
        railEl?.addEventListener("click", (e) => {
            const li = e.target.closest("li");
            if (!li) return;
            const i = [...railEl.children].indexOf(li);
            if (i >= 0) { auto = false; goTo(i); }
        }, sig);
        root.querySelectorAll("[data-probe]").forEach((btn) => {
            btn.addEventListener("click", () => runProbe(btn.getAttribute("data-probe")), sig);
        });

        const ro = new ResizeObserver(() => { buildEdges(); if (!moving) park(BEATS[beat].node); });
        ro.observe(graph);

        requestAnimationFrame(() => {
            buildEdges();
            requestAnimationFrame(() => {
                buildEdges();
                renderCap("disk");
                applyBeat(0);
                renderSpace();
                if (!reduce) raf = requestAnimationFrame((t) => { last = t; loop(t); });
            });
        });

        return {
            destroy() {
                dead = true;
                ac.abort();
                cancelAnimationFrame(raf);
                ro.disconnect();
                capEl.remove();
                flow.replaceChildren();
                svg.replaceChildren();
            }
        };
    }

    function boot() {
        if (ctl) { ctl.destroy(); ctl = null; }
        const root = document.getElementById("ingest-graph");
        if (!root) return;
        ctl = start(root);
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot);
    else boot();
    document.addEventListener("enhancedload", boot);
    if (window.Blazor && typeof Blazor.addEventListener === "function") {
        Blazor.addEventListener("enhancedload", boot);
    }
})();
