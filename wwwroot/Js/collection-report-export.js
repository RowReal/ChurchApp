
window.collectionReportDownloadCsv = (filename, content) => {
    const blob = new Blob(
        ["\uFEFF", content],
        { type: "text/csv;charset=utf-8" }
    );

    const url = URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = url;
    a.download = filename;

    document.body.appendChild(a);
    a.click();
    a.remove();

    setTimeout(() => URL.revokeObjectURL(url), 1000);
};
