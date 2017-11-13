
/** invoke a get for specific url
*/
function apiCall(url, onResultFn) {
    var xhr = new XMLHttpRequest()
    xhr.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            // Typical action to be performed when the document is ready:
            onResultFn(xhr.responseText)
        }
    };
    //xhr.onload = function () {
    //    onResultFn(xhr.responseText)
    //}
    xhr.open('GET', url, true)
    xhr.send()
}

function apiJSONCall(url, onResultFn) {
    apiCall(url, (responseText) => { onResultFn(JSON.parse(responseText)) })
}
