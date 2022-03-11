import { Deferred } from 'devextreme/core/utils/deferred';
import { HttpClient, HttpParams } from '@angular/common/http';

export function sendRequestFactory(httpClient: HttpClient) {
  const URLENCODED = 'application/x-www-form-urlencoded';
  const CONTENT_TYPE = 'Content-Type';

  let nonce = Date.now();

  function assignResponseProps(xhrSurrogate, response) {
    function getResponseHeader(name) {
      return response.headers.get(name);
    }

    function makeResponseText() {
      const body = 'error' in response ? response.error : response.body;

      if (typeof body !== 'string' || String(getResponseHeader(CONTENT_TYPE)).indexOf('application/json') === 0) {
          return JSON.stringify(body);
      }

      return body;
    }

    Object.assign(xhrSurrogate, {
      status: response.status,
      statusText: response.statusText,
      getResponseHeader,
      responseText: makeResponseText()
    });
    
    return xhrSurrogate;
  }

  function getAcceptHeader(options) {
    const dataType = options.dataType;
    const accepts = options.accepts;
    const fallback = ',*/*;q=0.01';

    if (dataType && accepts && accepts[dataType]) {
      return accepts[dataType] + fallback;
    }

    switch (dataType) {
      case 'json': return 'application/json, text/javascript' + fallback;
      case 'text': return 'text/plain' + fallback;
      case 'xml': return 'application/xml, text/xml' + fallback;
      case 'html': return 'text/html' + fallback;
      case 'script': return 'text/javascript, application/javascript, application/ecmascript, application/x-ecmascript' + fallback;
    }

    return '*/*';
}

  return (options) => {
    const d = Deferred();

    const method = (options.method || 'get').toLowerCase();
    const isGet = method === 'get';
    const headers = { ...options.headers };
    const data = options.data;
    const upload = options.upload;
    const beforeSend = options.beforeSend;
    const xhrFields = options.xhrFields;

    if (options.cache === false && isGet && data) {
      data._ = nonce++;
    }

    if (!headers.Accept) {
      headers.Accept = getAcceptHeader(options);
    }

    if (!upload && !isGet && !headers[CONTENT_TYPE]) {
      headers[CONTENT_TYPE] = options.contentType || URLENCODED + ';charset=utf-8';
    }

    let params;
    let body;

    if (isGet) {
        params = data;
    } else {
        if (!upload && typeof data === 'object' && headers[CONTENT_TYPE].indexOf(URLENCODED) === 0) {
            body = new HttpParams();
            // tslint:disable-next-line:forin
            for (const key in data) {
                body = body.set(key, data[key]);
            }
            body = body.toString();
        } else {
            body = data;
        }
    }

    const xhrSurrogate = { };

    if (beforeSend) {
      beforeSend(xhrSurrogate);
    }

    httpClient
      .request(
        method,
        options.url,
        {
          params,
          body,
          responseType: options.dataType,
          headers,
          withCredentials: xhrFields && xhrFields.withCredentials,
          observe: 'response'
        }
      )
      .subscribe(
        (response) => d.resolve(response.body, 'success', assignResponseProps(xhrSurrogate, response)),
        (response) => d.reject(assignResponseProps(xhrSurrogate, response), 'error')
      );

    return d.promise();
  };
}