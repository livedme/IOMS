function drawPdfHeader(pdf, pageW, margin, headerH, title, reference, logo) {
    let textX = margin;
    if (logo && logo.dataUrl) {
        const logoH = 7;
        const ratio = logo.width && logo.height ? (logo.width / logo.height) : 2;
        const logoW = logoH * ratio;
        try {
            pdf.addImage(logo.dataUrl, 'PNG', margin, margin + 1.5, logoW, logoH);
            textX = margin + logoW + 3;
        } catch (e) { /* keep text at margin if the logo cannot be drawn */ }
    }

    pdf.setFont('helvetica', 'bold');
    pdf.setFontSize(12);
    pdf.setTextColor(2, 21, 45);
    pdf.text('AVANCER', textX, margin + 6);

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(8);
    pdf.setTextColor(110, 116, 128);
    pdf.text('Inventory & Order Management', textX, margin + 11);

    if (title) {
        pdf.setFont('helvetica', 'bold');
        pdf.setFontSize(11);
        pdf.setTextColor(2, 21, 45);
        pdf.text(title, pageW - margin, margin + 6, { align: 'right' });
    }
    if (reference) {
        pdf.setFont('helvetica', 'normal');
        pdf.setFontSize(9);
        pdf.setTextColor(110, 116, 128);
        pdf.text(reference, pageW - margin, margin + 11, { align: 'right' });
    }

    pdf.setDrawColor(210, 215, 225);
    pdf.setLineWidth(0.3);
    pdf.line(margin, margin + headerH - 3, pageW - margin, margin + headerH - 3);
}

function drawPdfFooter(pdf, pageW, pageH, margin, footerH, page, total) {
    const lineY = pageH - margin - footerH + 3;
    pdf.setDrawColor(210, 215, 225);
    pdf.setLineWidth(0.3);
    pdf.line(margin, lineY, pageW - margin, lineY);

    const textY = lineY + 5;
    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(8);
    pdf.setTextColor(110, 116, 128);
    pdf.text('Avancer Inc. | 123 Business Park, New York, NY 10001, USA', margin, textY);
    pdf.text('Phone: (212) 555-0000 | sales@avancer.com | www.avancer.com', margin, textY + 4);
    pdf.text('Page ' + page + ' of ' + total, pageW - margin, textY, { align: 'right' });
}

function drawLedgerFooter(pdf, pageW, pageH, margin, footerH, page, total, showSignature) {
    const footerTop = pageH - margin - footerH;
    const lineY = pageH - margin - 12;

    if (showSignature) {
        const sigRight = pageW - margin;
        const sigLeft = sigRight - 55;
        const sigY = footerTop + 9;
        pdf.setDrawColor(17, 24, 39);
        pdf.setLineWidth(0.3);
        pdf.line(sigLeft, sigY, sigRight, sigY);
        pdf.setFont('helvetica', 'normal');
        pdf.setFontSize(7.5);
        pdf.setTextColor(55, 65, 81);
        pdf.text('Authorized Signature', (sigLeft + sigRight) / 2, sigY + 4, { align: 'center' });
    }

    pdf.setDrawColor(210, 215, 225);
    pdf.setLineWidth(0.3);
    pdf.line(margin, lineY, pageW - margin, lineY);

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(7.5);
    pdf.setTextColor(110, 116, 128);
    pdf.text('Manager (Accounts)', margin, lineY + 5);
    pdf.text('Page ' + page + ' of ' + total, pageW - margin, lineY + 5, { align: 'right' });
    pdf.text('Printed by :a', margin, lineY + 9);
    pdf.text('Powered by Avancer', pageW - margin, lineY + 9, { align: 'right' });
}

function drawLedgerHeader(pdf, pageW, margin, headerH, el) {
    const d = el.dataset || {};
    const account = d.ledgerAccount || 'All Customers';
    const from = d.ledgerFrom || 'All';
    const to = d.ledgerTo || 'Today';
    const top = margin;

    pdf.setTextColor(0, 0, 0);
    pdf.setFont('helvetica', 'bold');
    pdf.setFontSize(11);
    pdf.text('AVANCER', margin, top + 5);

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(7.5);
    pdf.text('123 Business Park, New York, NY 10001, USA', margin, top + 9);
    pdf.text('Ph. : (212) 555-0000', margin, top + 13);
    pdf.text('Mb. : -', margin, top + 17);

    pdf.setDrawColor(0, 0, 0);
    pdf.setLineWidth(0.4);
    pdf.line(margin, top + 19, pageW - margin, top + 19);

    pdf.setFont('helvetica', 'bold');
    pdf.setFontSize(10);
    pdf.text('LEDGER BOOK', margin, top + 25);
    pdf.setFontSize(8);
    pdf.text(d.ledgerReport || '', pageW / 2, top + 25, { align: 'center' });

    pdf.setFont('helvetica', 'normal');
    pdf.text('Date ' + from + ' To ' + to, pageW - margin, top + 25, { align: 'right' });

    pdf.setFont('helvetica', 'bold');
    pdf.setFontSize(8);
    pdf.text('Account', margin, top + 31);
    pdf.text('Account Name: ' + account, margin, top + 35);
    pdf.text('Mobile No.: -', margin, top + 39);

    pdf.setFont('helvetica', 'normal');
    pdf.text('Opening Tk. :', pageW - margin - 25, top + 45, { align: 'right' });
    pdf.text('0.00 Dr', pageW - margin, top + 45, { align: 'right' });

    pdf.line(margin, margin + headerH - 3, pageW - margin, margin + headerH - 3);
}

// Lazily load a stylesheet once. Resolves when it is present (either already loaded or after fetch).
function ensureCss(href) {
    return new Promise((resolve) => {
        const existing = document.querySelector('link[href="' + href + '"]');
        if (existing) {
            resolve();
            return;
        }
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.onload = () => resolve();
        link.onerror = () => resolve();
        document.head.appendChild(link);
    });
}

// Lazily load a script once. Resolves once it is available globally.
function ensureScript(src) {
    return new Promise((resolve) => {
        const ready = () => src.indexOf('html2canvas') !== -1 ? typeof html2canvas !== 'undefined'
            : !!(window.jspdf && window.jspdf.jsPDF);
        if (ready()) {
            resolve();
            return;
        }
        const existing = document.querySelector('script[src="' + src + '"]');
        if (existing) {
            resolve(); // the load event listener will have fired already if it made it here
            return;
        }
        const s = document.createElement('script');
        s.src = src;
        s.onload = () => resolve();
        s.onerror = () => resolve();
        document.head.appendChild(s);
    });
}

function ensurePdfAssets() {
    return Promise.all([
        ensureCss('/print.css'),
        ensureScript('https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js'),
        ensureScript('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js')
    ]);
}

// Load /logo.png once (as a data URL + intrinsic size) so it can be drawn into the PDF header.
let _logoPromise = null;
function loadLogo() {
    if (!_logoPromise) {
        _logoPromise = fetch('/logo.png')
            .then((r) => (r.ok ? r.blob() : Promise.reject()))
            .then((blob) => new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => {
                    const img = new Image();
                    img.onload = () => resolve({ dataUrl: reader.result, width: img.naturalWidth, height: img.naturalHeight });
                    img.onerror = reject;
                    img.src = reader.result;
                };
                reader.onerror = reject;
                reader.readAsDataURL(blob);
            }))
            .catch(() => null);
    }
    return _logoPromise;
}

function esc(value) {
    return (value || '').toString().replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// Repeating print bands (mirror the markup on the /print page).
function printChromeBands(title, reference) {
    return ''
        + '<div class="print-chrome print-header">'
        + '<div class="pc-left"><img src="/logo.png" class="pc-logo" alt="Avancer" />'
        + '<span class="pc-text"><span class="pc-brand">AVANCER</span>'
        + '<span class="pc-tag">Inventory &amp; Order Management</span></span></div>'
        + '<div class="pc-right"><span class="pc-title">' + esc(title) + '</span>'
        + '<span class="pc-ref">' + esc(reference) + '</span></div>'
        + '</div>'
        + '<div class="print-chrome print-footer">'
        + '<span>Avancer Inc. | 123 Business Park, New York, NY 10001, USA</span>'
        + '<span>Phone: (212) 555-0000 | sales@avancer.com | www.avancer.com</span>'
        + '</div>';
}

function getLedgerMeta(html) {
    try {
        const doc = new DOMParser().parseFromString(html || '', 'text/html');
        const page = doc.querySelector('#doc-page');
        if (!page || page.getAttribute('data-ledger') !== '1') return null;
        return {
            account: page.getAttribute('data-ledger-account') || 'All Customers',
            from: page.getAttribute('data-ledger-from') || 'All',
            to: page.getAttribute('data-ledger-to') || 'Today',
            report: page.getAttribute('data-ledger-report') || ''
        };
    } catch {
        return null;
    }
}

function ledgerPrintBands(meta) {
    return ''
        + '<div class="print-chrome print-header ledger-print-header">'
        + '<div class="lp-company"><div class="lp-brand">AVANCER</div>'
        + '<div class="lp-addr">123 Business Park, New York, NY 10001, USA</div>'
        + '<div class="lp-addr">Ph. : (212) 555-0000</div>'
        + '<div class="lp-addr">Mb. : -</div></div>'
        + '<div class="lp-rule"></div>'
        + '<div class="lp-report-row"><span class="lp-title">LEDGER BOOK</span>'
        + '<span class="lp-report">' + esc(meta.report) + '</span>'
        + '<span class="lp-date">Date ' + esc(meta.from) + ' To ' + esc(meta.to) + '</span></div>'
        + '<div class="lp-account-row"><span>Account</span>'
        + '<span>Account Name: ' + esc(meta.account) + '</span>'
        + '<span>Mobile No.: -</span></div>'
        + '<div class="lp-opening-row"><span>Opening Tk. :</span><span>0.00 Dr</span></div>'
        + '</div>'
        + '<div class="print-chrome print-footer ledger-print-footer">'
        + '<span>Manager (Accounts)</span><span>Printed by :a</span>'
        + '</div>';
}

window.iomsPdf = {
    // Receives a server-rendered HTML fragment (the document markup) and generates a PDF from it.
    // Used by the /print page and business panels so the server can build the template and hand
    // off only the markup.
    downloadHtml: async function (html, fileName, title, reference) {
        await ensurePdfAssets();

        const host = document.createElement('div');
        host.setAttribute('data-ioms-pdf-host', '1');
        host.style.position = 'fixed';
        host.style.left = '-10000px';
        host.style.top = '0';
        host.style.width = '820px';
        host.style.background = '#ffffff';
        host.innerHTML = html || '';
        document.body.appendChild(host);
        try {
            // The page may already hold a #doc-page element, so scope the lookup to this host.
            const el = host.querySelector('#doc-page') || host;
            return await this._renderElement(el, fileName, title, reference);
        } finally {
            host.remove();
        }
    },

    // Prints a server-rendered HTML fragment without navigation: loads it into an off-screen
    // same-origin iframe (with the repeating header/footer bands) and prints from there.
    printHtml: async function (html, title, reference, ledgerMeta) {
        await ensureCss('/print.css');

        const ledger = ledgerMeta || getLedgerMeta(html);
        const bands = ledger ? ledgerPrintBands(ledger) : printChromeBands(title, reference);
        const documentHtml = '<!DOCTYPE html><html><head><meta charset="utf-8">'
            + '<link rel="stylesheet" href="/print.css"></head><body'
            + (ledger ? ' class="ledger-print"' : '') + '>'
            + bands
            + (html || '')
            + '</body></html>';

        const previous = document.getElementById('ioms-print-frame');
        if (previous) previous.remove();

        await new Promise((resolve) => {
            const iframe = document.createElement('iframe');
            iframe.id = 'ioms-print-frame';
            iframe.setAttribute('aria-hidden', 'true');
            iframe.style.cssText = 'position:fixed;left:-10000px;top:0;width:820px;height:1100px;border:0;';

            // Assign onload + srcdoc *before* appending so the frame commits our markup directly
            // (no about:blank load) and the load event fires exactly once for this content.
            iframe.onload = () => {
                // Let print.css and the logo image settle before opening the print dialog.
                setTimeout(() => {
                    try {
                        iframe.contentWindow.focus();
                        iframe.contentWindow.print();
                    } finally {
                        resolve();
                    }
                }, 300);
            };
            iframe.srcdoc = documentHtml;
            document.body.appendChild(iframe);
        });
        return true;
    },

    _renderElement: async function (el, fileName, title, reference) {
        const ready = () => typeof html2canvas !== 'undefined' && window.jspdf && window.jspdf.jsPDF;
        for (let i = 0; i < 50 && !ready(); i++) {
            await new Promise(r => setTimeout(r, 100));
        }
        if (!ready()) return false;

        const logo = await loadLogo();

        // The document's own header/footer would only land on the first/last page, so hide
        // them and draw a repeating header + footer (with page numbers) on every page instead.
        const head = el.querySelector('.doc-head');
        const foot = el.querySelector('.doc-footer');
        const prevShadow = el.style.boxShadow;
        const prevBorder = el.style.border;
        const headDisplay = head ? head.style.display : null;
        const footDisplay = foot ? foot.style.display : null;

        el.style.boxShadow = 'none';
        el.style.border = 'none';
        if (head) head.style.display = 'none';
        if (foot) foot.style.display = 'none';

        let canvas;
        try {
            canvas = await html2canvas(el, {
                scale: 2,
                useCORS: true,
                backgroundColor: '#ffffff',
                windowWidth: el.scrollWidth,
                scrollX: 0,
                scrollY: -window.scrollY
            });
        } finally {
            el.style.boxShadow = prevShadow;
            el.style.border = prevBorder;
            if (head) head.style.display = headDisplay;
            if (foot) foot.style.display = footDisplay;
        }

        const { jsPDF } = window.jspdf;
        const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
        const pageW = pdf.internal.pageSize.getWidth();
        const pageH = pdf.internal.pageSize.getHeight();
        const isLedger = !!(el && el.dataset && el.dataset.ledger === '1');
        const isLedgerHeader = !!(el && el.dataset && el.dataset.ledgerHeader === '1');
        const margin = 10;
        const headerH = isLedgerHeader ? 50 : 16;
        const footerH = isLedger ? 34 : 14;
        const contentW = pageW - margin * 2;
        const contentTop = margin + headerH;
        const contentBottom = pageH - margin - footerH;
        const contentH = contentBottom - contentTop;

        const pxPerMm = canvas.width / contentW;
        const sliceHpx = Math.floor(contentH * pxPerMm);
        const totalPages = Math.max(1, Math.ceil(canvas.height / sliceHpx));

        let srcY = 0;
        for (let page = 0; page < totalPages; page++) {
            if (page > 0) pdf.addPage();

            const slicePx = Math.min(sliceHpx, canvas.height - srcY);
            const sliceHmm = slicePx / pxPerMm;

            const slice = document.createElement('canvas');
            slice.width = canvas.width;
            slice.height = slicePx;
            slice.getContext('2d').drawImage(canvas, 0, srcY, canvas.width, slicePx, 0, 0, canvas.width, slicePx);

            pdf.addImage(slice.toDataURL('image/jpeg', 0.95), 'JPEG', margin, contentTop, contentW, sliceHmm);
            if (isLedgerHeader) {
                drawLedgerHeader(pdf, pageW, margin, headerH, el);
            } else {
                drawPdfHeader(pdf, pageW, margin, headerH, title, reference, logo);
            }
            if (isLedger) {
                drawLedgerFooter(pdf, pageW, pageH, margin, footerH, page + 1, totalPages, page === totalPages - 1);
            } else {
                drawPdfFooter(pdf, pageW, pageH, margin, footerH, page + 1, totalPages);
            }

            srcY += slicePx;
        }

        pdf.save(fileName || 'document.pdf');
        return true;
    }
};

// Called from PrintDocument.razor / view panels via JS.InvokeVoidAsync("downloadFile", html, fileName, title, ref).
window.downloadFile = async function (html, fileName, title, reference) {
    const ok = await window.iomsPdf.downloadHtml(html, fileName, title, reference);
    if (!ok) {
        alert('PDF engine is still loading — please try again in a moment.');
    }
    return ok;
};

// Called from view panels via JS.InvokeVoidAsync("printHtml", html, title, ref).
window.printHtml = async function (html, title, reference) {
    await window.iomsPdf.printHtml(html, title, reference);
    return true;
};