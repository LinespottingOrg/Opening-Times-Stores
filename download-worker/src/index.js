/**
 * Partner Center Package URL host.
 * Canonical v1.0.0:
 *   https://downloads.linespotting.com/opening-times-stores/downloads/1.0.0/setup.exe
 * (Microsoft example shape: https://www.contoso.com/contosoapp/downloads/1.1/setup.exe)
 */
export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    let path = url.pathname;

    // Normalize trailing slash
    if (path.length > 1 && path.endsWith("/")) path = path.slice(0, -1);

    if (path === "/" || path === "") {
      return new Response(
        [
          "Opening Times EU — store hours + weather widget",
          "",
          "Privacy:",
          "https://downloads.linespotting.com/opening-times-stores/docs/privacy.html",
          "",
          "Windows setup (Partner Center Package URL):",
          "https://downloads.linespotting.com/opening-times-stores/downloads/1.0.2/setup.exe",
          "",
          "GitHub:",
          "https://github.com/LinespottingOrg/Opening-Times-Stores",
        ].join("\n"),
        { status: 200, headers: { "content-type": "text/plain; charset=utf-8" } }
      );
    }

    // Map short alias
    if (path === "/setup.exe" || path === "/1.0.2/setup.exe" || path === "/1.0.1/setup.exe" || path === "/1.0.0/setup.exe") {
      if (path === "/1.0.1/setup.exe")
        path = "/opening-times-stores/downloads/1.0.1/setup.exe";
      else if (path === "/1.0.0/setup.exe")
        path = "/opening-times-stores/downloads/1.0.0/setup.exe";
      else
        path = "/opening-times-stores/downloads/1.0.2/setup.exe";
    }

    // Docs HTML for Partner Center
    const docMap = {
      "/opening-times-stores/docs/installer-return-codes.html":
        "opening-times-stores/docs/installer-return-codes.html",
      "/docs/installer-return-codes.html":
        "opening-times-stores/docs/installer-return-codes.html",
      "/opening-times-stores/docs/privacy.html":
        "opening-times-stores/docs/privacy.html",
      "/docs/privacy.html": "opening-times-stores/docs/privacy.html",
      "/cookie-cleaner/docs/installer-return-codes.html":
        "cookie-cleaner/docs/installer-return-codes.html",
      "/cookie-cleaner/docs/privacy.html": "cookie-cleaner/docs/privacy.html",
      "/cookie-cleaner/docs/tos.html": "cookie-cleaner/docs/tos.html",
    };
    if (docMap[path]) {
      const doc = await env.BUCKET.get(docMap[path]);
      if (!doc) return new Response("Docs not found", { status: 404 });
      const headers = new Headers();
      headers.set("content-type", "text/html; charset=utf-8");
      headers.set("content-length", String(doc.size));
      headers.set("cache-control", "public, max-age=300");
      if (request.method === "HEAD") return new Response(null, { status: 200, headers });
      return new Response(doc.body, { status: 200, headers });
    }

    // Must look versioned + downloadable
    const ok =
      /^\/opening-times-stores\/downloads\/\d+\.\d+(\.\d+)?\/setup\.exe$/i.test(path) ||
      /^\/cookie-cleaner\/downloads\/\d+\.\d+(\.\d+)?\/setup\.exe$/i.test(path) ||
      /^\/downloads\/\d+\.\d+(\.\d+)?\/.+\.exe$/i.test(path);

    if (!ok) {
      return new Response(
        "Not found. Use versioned path e.g. /opening-times-stores/downloads/1.0.0/setup.exe",
        { status: 404, headers: { "content-type": "text/plain; charset=utf-8" } }
      );
    }

    const key = path.replace(/^\//, "");
    const obj = await env.BUCKET.get(key);
    if (!obj) {
      const legacy = await env.BUCKET.get(
        "downloads/1.0.0/OpeningTimesStores-Setup-1.0.0.exe"
      );
      if (!legacy) {
        return new Response("File not found: " + key, { status: 404 });
      }
      return serve(legacy, "OpeningTimesStores-Setup-1.0.0.exe", request.method);
    }

    return serve(obj, "setup.exe", request.method);
  },
};

function serve(obj, filename, method) {
  const headers = new Headers();
  obj.writeHttpMetadata(headers);
  headers.set("etag", obj.httpEtag);
  headers.set("content-type", "application/octet-stream");
  headers.set("content-length", String(obj.size));
  headers.set("content-disposition", `attachment; filename="${filename}"`);
  headers.set("cache-control", "public, max-age=3600");
  headers.set("access-control-allow-origin", "*");
  headers.set("access-control-allow-methods", "GET, HEAD, OPTIONS");

  if (method === "HEAD" || method === "OPTIONS") {
    return new Response(null, { status: 200, headers });
  }
  return new Response(obj.body, { status: 200, headers });
}
