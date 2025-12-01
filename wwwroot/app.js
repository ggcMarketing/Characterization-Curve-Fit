class MastersFinder {
    constructor() {
        this.masters = [25.46138, 47.649871, 97.231679, 201.233367, 411.27028, 804.700661, 1609.396284, 3291.780024];
        this.ranges = [];
        this.selectedRange = 0;
        this.order = 6;
        this.maxPct = 3;
        this.result = null;
        this.logs = [{ m: 'Ready. Load Magazine Values to begin.', t: 'info', ts: this.getTime() }];
        this.running = false;
        this.magOk = false;
        this.calOk = false;
        this.exportFormat = 'ini';
        
        this.render();
    }
    
    getTime() {
        return new Date().toLocaleTimeString();
    }
    
    log(message, type = 'info') {
        this.logs.push({ m: message, t: type, ts: this.getTime() });
        if (this.logs.length > 20) this.logs.shift();
        this.render();
    }
    
    async loadMagazine(file) {
        try {
            const formData = new FormData();
            formData.append('file', file);
            
            const response = await fetch('/api/parse-magazine', {
                method: 'POST',
                body: formData
            });
            
            const data = await response.json();
            if (data.values && data.values.length > 0) {
                this.masters = data.values;
                this.magOk = true;
                this.result = null;
                this.log(`✓ Loaded ${data.values.length} masters from ${file.name}`, 'success');
            } else {
                this.log('✗ No valid data found', 'error');
            }
        } catch (err) {
            this.log(`✗ Error loading magazine: ${err.message}`, 'error');
        }
        this.render();
    }
    
    async loadCalibration(file) {
        if (!this.magOk) {
            this.log('⚠ Load Magazine Values first!', 'warn');
            return;
        }
        
        try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('masters', JSON.stringify(this.masters));
            
            const response = await fetch('/api/parse-calibration', {
                method: 'POST',
                body: formData
            });
            
            const data = await response.json();
            if (data.ranges && data.ranges.length > 0) {
                this.ranges = data.ranges;
                this.selectedRange = 0;
                this.calOk = true;
                this.result = null;
                const totalPts = data.ranges.reduce((s, r) => s + r.pts.length, 0);
                this.log(`✓ Loaded ${data.ranges.length} range(s), ${totalPts} points from ${file.name}`, 'success');
            } else {
                this.log('✗ No valid calibration data found', 'error');
            }
        } catch (err) {
            this.log(`✗ Error loading calibration: ${err.message}`, 'error');
        }
        this.render();
    }
    
    generateSample() {
        const combos = this.calcCombos(this.masters);
        const maxIdx = Math.min(255, Math.pow(2, this.masters.length) - 1);
        const indices = Array.from({ length: maxIdx }, (_, i) => i + 1).filter(i => combos[i] > 0);
        const selected = indices.filter((_, i) => i % Math.max(1, Math.floor(indices.length / 15)) === 0).slice(0, 20);
        
        const points = selected.map(idx => ({
            v: 4.5 * Math.exp(-0.0006 * combos[idx]) + 0.15 + (Math.random() - 0.5) * 0.002,
            t: combos[idx],
            idx: idx
        })).sort((a, b) => a.t - b.t);
        
        this.ranges = [{ id: 1, pts: points }];
        this.selectedRange = 0;
        this.calOk = true;
        this.result = null;
        this.log(`✓ Generated ${points.length} sample points`, 'success');
        this.render();
    }
    
    calcCombos(masters) {
        const n = masters.length;
        const num = Math.pow(2, n);
        const combos = new Array(num).fill(0);
        
        for (let i = 0; i < num; i++) {
            for (let j = 0; j < n; j++) {
                if (i & (1 << j)) {
                    combos[i] += masters[j];
                }
            }
        }
        
        return combos;
    }
    
    async optimize() {
        if (!this.ranges.length) {
            this.log('⚠ Load calibration data first', 'warn');
            return;
        }
        
        this.running = true;
        this.log('Optimizing...');
        this.render();
        
        try {
            const t0 = performance.now();
            
            const response = await fetch('/api/optimize', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    masters: this.masters,
                    ranges: this.ranges,
                    order: this.order,
                    maxPct: this.maxPct
                })
            });
            
            this.result = await response.json();
            this.running = false;
            
            const elapsed = ((performance.now() - t0) / 1000).toFixed(3);
            this.log(`✓ Done in ${elapsed}s`, 'success');
            this.log(`R²: ${this.result.origRSq.toFixed(8)} → ${this.result.optRSq.toFixed(8)}`, 
                     this.result.imp >= 0 ? 'success' : 'warn');
        } catch (err) {
            this.running = false;
            this.log(`✗ Optimization error: ${err.message}`, 'error');
        }
        
        this.render();
    }
    
    apply() {
        if (this.result) {
            this.masters = [...this.result.opt];
            this.result = null;
            this.log('✓ Applied optimized values', 'success');
            this.render();
        }
    }
    
    async exportFile(format = 'legacy') {
        if (!this.result) return;
        
        try {
            const response = await fetch('/api/export-magazine', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    masters: this.result.opt,
                    format: format
                })
            });
            
            const data = await response.json();
            const blob = new Blob([data.content], { type: 'text/plain' });
            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = data.filename;
            a.click();
            
            this.log(`✓ Exported to ${data.filename}`, 'success');
        } catch (err) {
            this.log(`✗ Export error: ${err.message}`, 'error');
        }
    }
    
    updateMaster(index, value) {
        this.masters[index] = parseFloat(value) || 0;
        this.result = null;
        this.render();
    }
    
    getPctClass(pct) {
        return Math.abs(pct) > 2 ? 'pct-bad' : Math.abs(pct) > 1 ? 'pct-warn' : 'pct-good';
    }
    
    render() {
        const root = document.getElementById('root');
        const currentRange = this.ranges[this.selectedRange];
        
        root.innerHTML = `
            <div class="container">
                <div class="header">
                    <div>
                        <h1>🔬 Ion Chamber Calibration: Masters-Finder!</h1>
                        <div class="subtitle">Fixed kV Compatible • No Re-measurement Needed Between Iterations</div>
                    </div>
                    <div class="status">
                        <span class="${this.magOk ? 'status-ok' : 'status-no'}">Magazine ${this.magOk ? '✓' : '✗'}</span>
                        <span class="${this.calOk ? 'status-ok' : 'status-no'}">Cal Data ${this.calOk ? '✓' : '✗'}</span>
                    </div>
                </div>
                <div class="main">
                    <div class="info-box">
                        <strong>Fixed kV Mode:</strong> Take ONE measurement at fixed kV, then optimize as many times as needed without re-measuring. The optimizer adjusts master thicknesses mathematically.
                    </div>
                    <div class="btn-row">
                        <label class="btn btn-blue">
                            📂 Load Magazine Values
                            <input type="file" accept=".txt,.ini" id="mag-file" style="display:none">
                        </label>
                        <label class="btn ${this.magOk ? 'btn-orange' : 'btn-gray'}" style="opacity: ${this.magOk ? 1 : 0.5}">
                            📊 Load Cal Coeffs / Cal Data
                            <input type="file" accept=".txt,.csv,.ini" id="cal-file" style="display:none" ${!this.magOk ? 'disabled' : ''}>
                        </label>
                        <button class="btn btn-gray" id="sample-btn" ${!this.magOk ? 'disabled' : ''}>🎲 Sample Data</button>
                        <div class="spacer"></div>
                        <button class="btn btn-green" id="opt-btn" ${this.running || !this.calOk ? 'disabled' : ''}>
                            ${this.running ? '⏳ Running...' : '🚀 Optimize'}
                        </button>
                        <button class="btn btn-gray" id="apply-btn" ${!this.result ? 'disabled' : ''}>✅ Apply</button>
                        <button class="btn btn-gray" id="export-btn" ${!this.result ? 'disabled' : ''}>💾 Export</button>
                        ${this.result ? `
                            <select id="export-format" style="padding: 6px 8px; border: 1px solid #e2e8f0; border-radius: 4px; font-size: 0.85rem;">
                                <option value="ini" ${this.exportFormat === 'ini' ? 'selected' : ''}>INI Format</option>
                                <option value="legacy" ${this.exportFormat === 'legacy' ? 'selected' : ''}>Legacy Format</option>
                            </select>
                        ` : ''}
                    </div>
                    <div class="settings">
                        <label>Polynomial Order:
                            <select id="order-sel">
                                ${[3, 4, 5, 6, 7, 8].map(o => `<option value="${o}" ${o === this.order ? 'selected' : ''}>${o}</option>`).join('')}
                            </select>
                        </label>
                        <label>Max Correction:
                            <input type="number" id="maxpct-inp" value="${this.maxPct}" step="0.5" min="0.5" max="10">
                            %
                        </label>
                        ${this.result ? `
                            <div class="spacer"></div>
                            <div class="rsq-display">
                                R²: ${this.result.origRSq.toFixed(8)}
                                <span class="arrow">→</span>
                                <span class="optimized">${this.result.optRSq.toFixed(8)}</span>
                                <span class="delta ${this.result.imp >= 0 ? 'delta-pos' : 'delta-neg'}">
                                    ${this.result.imp >= 0 ? '+' : ''}${(this.result.imp * 1e6).toFixed(1)} ppm
                                </span>
                            </div>
                        ` : ''}
                    </div>
                    <div class="grid">
                        <div class="panel">
                            <div class="panel-head">
                                Master Sample Values
                                <span class="sub">${this.masters.length} masters</span>
                            </div>
                            <div class="panel-body">
                                <div class="scroll-table">
                                    <table>
                                        <thead>
                                            <tr><th style="width:30px">#</th><th>Current Value</th><th>Optimized</th><th style="width:70px">Change</th></tr>
                                        </thead>
                                        <tbody>
                                            ${this.masters.map((v, i) => `
                                                <tr>
                                                    <td style="font-weight:600; color:#718096">${i + 1}</td>
                                                    <td>
                                                        <input type="number" class="master-inp" data-idx="${i}" value="${v.toFixed(6)}" step="0.001">
                                                    </td>
                                                    <td>
                                                        <input class="optimized" value="${this.result ? this.result.opt[i].toFixed(6) : '--'}" readonly>
                                                    </td>
                                                    <td>
                                                        ${this.result ? `<span class="pct ${this.getPctClass(this.result.pct[i])}">
                                                            ${this.result.pct[i] >= 0 ? '+' : ''}${this.result.pct[i].toFixed(2)}%
                                                        </span>` : ''}
                                                    </td>
                                                </tr>
                                            `).join('')}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                        <div>
                            <div class="panel" style="margin-bottom:12px">
                                <div class="panel-head">
                                    Calibration Data
                                    <span class="sub">${this.ranges.reduce((s, r) => s + r.pts.length, 0)} points</span>
                                </div>
                                <div class="panel-body">
                                    ${this.ranges.length > 1 ? `
                                        <div class="range-tabs">
                                            ${this.ranges.map((r, i) => `
                                                <button class="range-tab ${this.selectedRange === i ? 'active' : ''}" data-range="${i}">
                                                    Range ${r.id} (${r.pts.length})
                                                </button>
                                            `).join('')}
                                        </div>
                                    ` : ''}
                                    ${currentRange ? `
                                        <div class="scroll-table" style="max-height:160px">
                                            <table class="cal-table">
                                                <thead><tr><th>#</th><th>Voltage</th><th>Thickness</th><th>Index</th></tr></thead>
                                                <tbody>
                                                    ${currentRange.pts.map((p, i) => `
                                                        <tr>
                                                            <td style="color:#a0aec0">${i + 1}</td>
                                                            <td>${p.v.toFixed(6)}</td>
                                                            <td>${p.t.toFixed(3)}</td>
                                                            <td style="color:#a0aec0">${p.idx}</td>
                                                        </tr>
                                                    `).join('')}
                                                </tbody>
                                            </table>
                                        </div>
                                    ` : `
                                        <div class="empty">No calibration data loaded.<br/>Load a file or click "Sample Data"</div>
                                    `}
                                </div>
                            </div>
                            <div class="panel">
                                <div class="panel-head">Log</div>
                                <div class="panel-body" style="padding:0">
                                    <div class="log">
                                        ${this.logs.map(l => `<div class="log-${l.t}">[${l.ts}] ${l.m}</div>`).join('')}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    ${this.result ? `
                        <div class="result-bar ${this.result.imp >= 0 ? 'result-ok' : 'result-warn'}">
                            ${this.result.imp >= 0 ? '✅ Optimization successful - R² improved' : '⚠️ R² did not improve - try adjusting polynomial order or check data quality'}
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
        
        // Attach event listeners
        document.getElementById('mag-file')?.addEventListener('change', e => {
            if (e.target.files[0]) this.loadMagazine(e.target.files[0]);
            e.target.value = '';
        });
        
        document.getElementById('cal-file')?.addEventListener('change', e => {
            if (e.target.files[0]) this.loadCalibration(e.target.files[0]);
            e.target.value = '';
        });
        
        document.getElementById('sample-btn')?.addEventListener('click', () => this.generateSample());
        document.getElementById('opt-btn')?.addEventListener('click', () => this.optimize());
        document.getElementById('apply-btn')?.addEventListener('click', () => this.apply());
        document.getElementById('export-btn')?.addEventListener('click', () => this.exportFile(this.exportFormat));
        
        document.getElementById('export-format')?.addEventListener('change', e => {
            this.exportFormat = e.target.value;
        });
        
        document.getElementById('order-sel')?.addEventListener('change', e => {
            this.order = parseInt(e.target.value);
        });
        
        document.getElementById('maxpct-inp')?.addEventListener('change', e => {
            this.maxPct = parseFloat(e.target.value) || 3;
        });
        
        document.querySelectorAll('.master-inp').forEach(inp => {
            inp.addEventListener('change', e => {
                const idx = parseInt(e.target.dataset.idx);
                this.updateMaster(idx, e.target.value);
            });
        });
        
        document.querySelectorAll('.range-tab').forEach(btn => {
            btn.addEventListener('click', e => {
                this.selectedRange = parseInt(e.target.dataset.range);
                this.render();
            });
        });
    }
}

// Initialize app
new MastersFinder();
