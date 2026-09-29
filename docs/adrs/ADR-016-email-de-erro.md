# ADR-016 — O e-mail de erro sai depois do status

## Status

Aceita.

## Contexto

A fila `status` já grava `erro` no Postgres. O dono ainda não era avisado. Faltava decidir quem envia, quando, para qual endereço e o que acontece se o envio falha.

## Decisão

A Video API envia o e-mail. O processor não envia. O destinatário é o e-mail gravado no registro do vídeo. Não há consulta à Auth.

O envio acontece depois que o Postgres já gravou `erro`, e só quando essa gravação mudou o status. `comecou` e `sucesso` não enviam. Mensagem repetida não reabre vídeo `concluido` ou `erro` e não envia de novo.

O assunto é `FIAP X: o vídeo não foi processado`. O corpo leva o identificador do vídeo. O remetente local é `fiapx@fiapx.local`.

Se o SMTP falha ou o envio é cancelado com o status já gravado, o vídeo permanece `erro`. A mensagem da fila `status` é confirmada. Não nasce outra fila para o e-mail. O cancelamento durante a gravação do status continua recolocando a mensagem.

No local, o SMTP é o Mailpit no Compose. A mensagem não sai da máquina. O painel fica em `http://localhost:8025` e o SMTP na porta `1025`.

O contador `fiapx_video_email` sai no `GET /metrics` que já existe, com o rótulo `resultado` (`enviado` ou `falhou`). No Prometheus ele aparece como `fiapx_video_email_total`. O dashboard `FIAP X` ganha o painel desse contador. Não nasce outro stack.

## Consequências

Depois de um processamento com erro, o painel do Mailpit mostra a mensagem para o e-mail do dono. O status no Postgres continua `erro` mesmo se o envio falhar.

O teste unitário usa envio de mentira. Não sobe PostgreSQL, MinIO, RabbitMQ, Redis, Mailpit, Prometheus nem Grafana.

Baixar o vídeo original, publicação da imagem e deploy continuam fora.
