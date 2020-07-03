var TextDownloaderPlugin = {

    TextDownloader: function(str, fn) {

        var msg = Pointer_stringify(str);
        var fname = Pointer_stringify(fn);
        var data = new Blob([msg], {type: 'text/plain'});
        var link = document.createElement('a');
        link.download = fname;
        link.innerHTML = 'DownloadFile';
        link.setAttribute('id', 'TextDownloaderLink');
        if(window.webkitURL != null)
        {
            link.href = window.webkitURL.createObjectURL(data);
        }
        else
        {
            link.href = window.URL.createObjectURL(data);
            link.onclick = function()
            {
                var child = document.getElementById('TextDownloaderLink');
                child.parentNode.removeChild(child);
            };
            link.style.display = 'none';
            document.body.appendChild(link);
        }
        link.click();
    }
};

mergeInto(LibraryManager.library, TextDownloaderPlugin);


var ImageDownloaderPlugin = {
    ImageDownloader: function(str, fn) {
        var msg = Pointer_stringify(str);
        var fname = Pointer_stringify(fn);
        var contentType = 'image/jpeg';
        function fixBinary (bin)
        {
            var length = bin.length;
            var buf = new ArrayBuffer(length);
            var arr = new Uint8Array(buf);
            for (var i = 0; i < length; i++)
            {
                arr[i] = bin.charCodeAt(i);
            }
            return buf;
        }
        var binary = fixBinary(atob(msg));
        var data = new Blob([binary], {type: contentType});
        var link = document.createElement('a');
        link.download = fname;
        link.innerHTML = 'DownloadFile';
        link.setAttribute('id', 'ImageDownloaderLink');
        if(window.webkitURL != null)
        {
            link.href = window.webkitURL.createObjectURL(data);
        }
        else
        {
            link.href = window.URL.createObjectURL(data);
            link.onclick = function()
            {
                var child = document.getElementById('ImageDownloaderLink');
                child.parentNode.removeChild(child);
            };
            link.style.display = 'none';
            document.body.appendChild(link);
        }
        link.click();
    }
};
mergeInto(LibraryManager.library, ImageDownloaderPlugin);