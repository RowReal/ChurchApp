window.bccQuillEditors = {};

window.bccQuill = {
    init: function (editorId, initialHtml) {
        const editorElement = document.getElementById(editorId);

        if (!editorElement) {
            return false;
        }

        const existingEditor = window.bccQuillEditors[editorId];

        if (
            existingEditor &&
            existingEditor.container &&
            existingEditor.container.isConnected
        ) {
            return true;
        }

        delete window.bccQuillEditors[editorId];

        const quill = new Quill('#' + editorId, {
            theme: 'snow',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    [{ 'header': [1, 2, 3, false] }],
                    ['link'],
                    ['clean']
                ]
            }
        });

        if (initialHtml) {
            quill.root.innerHTML = initialHtml;
        }

        window.bccQuillEditors[editorId] = quill;

        return true;
    },

    getHtml: function (editorId) {
        const quill = window.bccQuillEditors[editorId];

        if (!quill) {
            return "";
        }

        return quill.root.innerHTML;
    },

    setHtml: function (editorId, html) {
        const quill = window.bccQuillEditors[editorId];

        if (!quill) {
            return false;
        }

        quill.root.innerHTML = html || "";
        return true;
    },

    clear: function (editorId) {
        const quill = window.bccQuillEditors[editorId];

        if (!quill) {
            return false;
        }

        quill.setText("");
        return true;
    }
};