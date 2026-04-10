export default {
    async fetch(request) {
        const url = new URL(request.url);

        if (url.pathname === "/") {
            return Response.json({ mensagem: "API rodando 🚀" });
        }

        if (url.pathname === "/teste") {
            return Response.json({ ok: true });
        }

        return new Response("Not found", { status: 404 });
    }
};