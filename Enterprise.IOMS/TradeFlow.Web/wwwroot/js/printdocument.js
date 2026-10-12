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
    printHtml: async function (html, title, reference) {
        await ensureCss('/print.css');

        const documentHtml = '<!DOCTYPE html><html><head><meta charset="utf-8">'
            + '<link rel="stylesheet" href="/print.css"></head><body>'
            + printChromeBands(title, reference)
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
        const margin = 10;
        const headerH = 16;
        const footerH = 14;
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
            drawPdfHeader(pdf, pageW, margin, headerH, title, reference, logo);
            drawPdfFooter(pdf, pageW, pageH, margin, footerH, page + 1, totalPages);

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