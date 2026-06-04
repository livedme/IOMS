window.RichTextEditor = {
    editors: {},

    initialize: function (elementId, initialValue, dotNetRef) {
        var container = document.getElementById(elementId);
        if (!container) return;

        var quill = new Quill(container, {
            theme: 'snow',
            placeholder: container.getAttribute('data-placeholder') || '',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    [{ 'header': [1, 2, 3, false] }],
                    ['link', 'clean']
                ]
            }
        });

        if (initialValue) {
            quill.root.innerHTML = initialValue;
        }

        quill.on('text-change', function () {
            var html = quill.root.innerHTML;
            if (html === '<p><br></p>') html = '';
            dotNetRef.invokeMethodAsync('OnContentChanged', html);
        });

        this.editors[elementId] = quill;
    },

    setContent: function (elementId, content) {
        var q = this.editors[elementId];
        if (q) q.root.innerHTML = content || '';
    },

    dispose: function (elementId) {
        delete this.editors[elementId];
    }
};
