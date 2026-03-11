mergeInto(LibraryManager.library, {
    ReplayFilePicker_OpenFileDialog: function(gameObjectNamePtr, callbackMethodPtr, acceptPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);
        var accept = UTF8ToString(acceptPtr);

        var input = document.createElement('input');
        input.type = 'file';
        input.accept = accept;
        input.style.display = 'none';
        document.body.appendChild(input);

        input.onchange = function(e) {
            var file = e.target.files[0];
            document.body.removeChild(input);

            if (!file) {
                SendMessage(gameObjectName, callbackMethod, '');
                return;
            }

            var reader = new FileReader();
            reader.onload = function(event) {
                var base64 = btoa(
                    new Uint8Array(event.target.result)
                        .reduce(function(data, byte) {
                            return data + String.fromCharCode(byte);
                        }, '')
                );
                // Формат: fileName|base64Data
                var result = file.name + '|' + base64;
                SendMessage(gameObjectName, callbackMethod, result);
            };
            reader.onerror = function() {
                SendMessage(gameObjectName, callbackMethod, '');
            };
            reader.readAsArrayBuffer(file);
        };

        input.oncancel = function() {
            document.body.removeChild(input);
            SendMessage(gameObjectName, callbackMethod, '');
        };

        input.click();
    }
});