function drawPdfHeader(pdf, pageW, margin, headerH, title, reference) {
    pdf.setFont('helvetica', 'bold');
    pdf.setFontSize(12);
    pdf.setTextColor(2, 21, 45);
    pdf.text('AVANCER', margin, margin + 6);

    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(8);
    pdf.setTextColor(110, 116, 128);
    pdf.text('Inventory & Order Management', margin, margin + 11);

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

window.iomsPdf = {
    // Loads the print route in an off-screen same-origin iframe. The page's ?action query
    // decides whether it prints or downloads - no navigation, no new tab either way.
    openDocument: function (url) {
        debugger;
        let frame = document.getElementById('ioms-pdf-frame');
        if (!frame) {
        debugger;
            frame = document.createElement('iframe');
            frame.id = 'ioms-pdf-frame';
            frame.setAttribute('aria-hidden', 'true');
            frame.style.position = 'fixed';
            frame.style.left = '-10000px';
            frame.style.top = '0';
            frame.style.width = '820px';
            frame.style.height = '1100px';
            frame.style.border = '0';
            // /print is behind [Authorize]. If the session has expired the request 302s to the sign-in
            // page, which would load silently in this off-screen frame and make Print/Download look
            // broken. Send the user to sign in at the top level (the server-supplied ReturnUrl brings
            // them back to the document afterwards) instead of leaving them with a blank result.
            // frame.addEventListener('load', function () {
            //     try {
            //         debugger;
            //         const path = frame.contentWindow.location.pathname.toLowerCase();
            //         if (path.indexOf('/account/login') === 0) {
            //             window.top.location.href = frame.contentWindow.location.href;
            //         }
            //     } catch (e) {
            //        // Frame is not same-origin (unexpected); nothing to inspect.
            //     }
            // });
            document.body.appendChild(frame);
        }
        frame.src = url;
    },

    download: async function (elementId, fileName, title, reference) {
        debugger;
        const el = document.getElementById(elementId);
        if (!el) return false;
        return await window.iomsPdf._renderElement(el, fileName, title, reference);
    },

    // Receives a server-rendered HTML fragment (the document markup) and generates a PDF from it.
    // Used by the /print page so the server can build the template and hand off only the markup.
    downloadHtml: async function (html, fileName, title, reference) {
        debugger;
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
            // The page already holds a #doc-page element, so scope the lookup to this host.
            const el = host.querySelector('#doc-page') || host;
            return await window.iomsPdf._renderElement(el, fileName, title, reference);
        } finally {
            host.remove();
        }
    },

    _renderElement: async function (el, fileName, title, reference) {
        const ready = () => typeof html2canvas !== 'undefined' && window.jspdf && window.jspdf.jsPDF;
        for (let i = 0; i < 50 && !ready(); i++) {
            await new Promise(r => setTimeout(r, 100));
        }
        if (!ready()) return false;

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
            drawPdfHeader(pdf, pageW, margin, headerH, title, reference);
            drawPdfFooter(pdf, pageW, pageH, margin, footerH, page + 1, totalPages);

            srcY += slicePx;
        }

        pdf.save(fileName || 'document.pdf');
        return true;
    }
};

// Called from PrintDocument.razor via JS.InvokeVoidAsync("downloadFile", html, fileName, title, ref).
window.downloadFile = async function (html, fileName, title, reference) {
    debugger;
    const ok = await window.iomsPdf.downloadHtml(html, fileName, title, reference);
    if (!ok) {
        alert('PDF engine is still loading — please try again in a moment.');
    }
    return ok;
};
