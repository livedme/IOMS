window.iomsPdf = {
    download: async function (elementId, fileName) {
        const el = document.getElementById(elementId);
        if (!el) return false;
        if (typeof html2canvas === 'undefined' || !window.jspdf || !window.jspdf.jsPDF) return false;

        const prevShadow = el.style.boxShadow;
        const prevBorder = el.style.border;
        el.style.boxShadow = 'none';
        el.style.border = 'none';

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
        }

        const { jsPDF } = window.jspdf;
        const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
        const pageW = pdf.internal.pageSize.getWidth();
        const pageH = pdf.internal.pageSize.getHeight();
        const margin = 8;
        const usableH = pageH - margin * 2;
        const imgW = pageW - margin * 2;
        const imgH = canvas.height * imgW / canvas.width;
        const imgData = canvas.toDataURL('image/jpeg', 0.95);

        let heightLeft = imgH;
        let position = margin;
        pdf.addImage(imgData, 'JPEG', margin, position, imgW, imgH);
        heightLeft -= usableH;

        while (heightLeft > 0.5) {
            position = margin - (imgH - heightLeft);
            pdf.addPage();
            pdf.addImage(imgData, 'JPEG', margin, position, imgW, imgH);
            heightLeft -= usableH;
        }

        pdf.save(fileName || 'document.pdf');
        return true;
    }
};
