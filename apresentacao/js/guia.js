const ordem = [
  "index.html",
  "arquitetura.html",
  "dominio.html",
  "nuvem.html",
  "tecnologias.html",
  "executar.html",
];

const caminho = location.pathname.replaceAll("\\", "/");
const atual = ordem.find((nome) => caminho.endsWith("/" + nome) || caminho.endsWith(nome));

document.querySelectorAll(".menu a").forEach((link) => {
  if (link.getAttribute("href") === atual) {
    link.setAttribute("aria-current", "page");
  }
});

document.addEventListener("keydown", (evento) => {
  if (evento.altKey || evento.ctrlKey || evento.metaKey) return;
  const indice = ordem.indexOf(atual);
  if (indice < 0) return;
  if (evento.key === "ArrowRight" && indice < ordem.length - 1) {
    location.href = ordem[indice + 1];
  }
  if (evento.key === "ArrowLeft" && indice > 0) {
    location.href = ordem[indice - 1];
  }
});
