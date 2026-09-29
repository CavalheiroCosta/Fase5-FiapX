# Web

Front de demonstração. Não altera as APIs.

O login da conta `Adm` abre o cadastro de usuários e os atalhos do Grafana, do Prometheus, do MinIO, do RabbitMQ e do Mailpit. Qualquer outro login envia vídeo, lista o andamento e baixa o ZIP quando o status é `concluido`.

O Compose sobe esta página em `http://localhost:5173`. O Vite encaminha `/auth` para a Auth e `/video` para a Video API.

```powershell
docker compose up -d
```

Fora do Compose, com a Auth em `http://localhost:5298` e a Video em `http://localhost:5299`:

```powershell
npm install
npm run dev
```

A conta pronta é `Adm` / `Adm`.
