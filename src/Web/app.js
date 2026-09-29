const CHAVE = 'fiapx.token'
const LOGIN_ADM = 'Adm'
const ABERTOS = new Set(['aguardando_processamento', 'em_processamento'])

const statusTexto = {
  aguardando_processamento: 'Aguardando processamento',
  em_processamento: 'Em processamento',
  concluido: 'Concluído',
  erro: 'Erro',
}

const aviso = document.querySelector('#aviso')
const titulo = document.querySelector('#titulo')
const botaoSair = document.querySelector('#sair')
const telaLogin = document.querySelector('#tela-login')
const telaAdm = document.querySelector('#tela-adm')
const telaVideos = document.querySelector('#tela-videos')
const listaUsuarios = document.querySelector('#lista-usuarios')
const listaVideos = document.querySelector('#lista-videos')
const formAlterar = document.querySelector('#form-alterar')
const notaErro = document.querySelector('#nota-erro')
const vazio = document.querySelector('#vazio')

let temporizador = null
let usuarioEditado = null

document.querySelector('#form-login').addEventListener('submit', entrar)
document.querySelector('#form-criar').addEventListener('submit', criarUsuario)
document.querySelector('#form-alterar').addEventListener('submit', salvarUsuario)
document.querySelector('#form-envio').addEventListener('submit', enviarVideo)
document.querySelector('#cancelar-alterar').addEventListener('click', fecharAlteracao)
document.querySelector('#atualizar').addEventListener('click', () => carregarVideos())
botaoSair.addEventListener('click', () => {
  sessionStorage.removeItem(CHAVE)
  mostrarLogin()
})

iniciar()

function iniciar() {
  const token = sessionStorage.getItem(CHAVE)
  const login = token ? loginDoToken(token) : null
  if (!login) {
    sessionStorage.removeItem(CHAVE)
    mostrarLogin()
    return
  }

  abrirArea(login)
}

function mostrarLogin() {
  pararLista()
  usuarioEditado = null
  formAlterar.hidden = true
  titulo.textContent = 'Entrar'
  botaoSair.hidden = true
  telaLogin.hidden = false
  telaAdm.hidden = true
  telaVideos.hidden = true
  limparAviso()
}

function abrirArea(login) {
  botaoSair.hidden = false
  telaLogin.hidden = true
  limparAviso()

  if (login === LOGIN_ADM) {
    titulo.textContent = 'Cadastro'
    telaAdm.hidden = false
    telaVideos.hidden = true
    pararLista()
    carregarUsuarios()
    return
  }

  titulo.textContent = 'Vídeos'
  telaAdm.hidden = true
  telaVideos.hidden = false
  carregarVideos()
}

async function entrar(evento) {
  evento.preventDefault()
  limparAviso()
  const form = evento.currentTarget
  const dados = new FormData(form)
  const resposta = await fetch('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      login: dados.get('login'),
      senha: dados.get('senha'),
    }),
  })

  if (!resposta.ok) {
    mostrarAviso(await mensagem(resposta))
    return
  }

  const corpo = await resposta.json()
  const login = loginDoToken(corpo.token)
  if (!login) {
    mostrarAviso('Não foi possível ler o token.')
    return
  }

  sessionStorage.setItem(CHAVE, corpo.token)
  form.reset()
  abrirArea(login)
}

function sairPorAcesso() {
  sessionStorage.removeItem(CHAVE)
  mostrarLogin()
  mostrarAviso('Acesso recusado.')
}

async function pedir(caminho, opcoes = {}) {
  const headers = new Headers(opcoes.headers)
  const token = sessionStorage.getItem(CHAVE)
  if (token)
    headers.set('Authorization', `Bearer ${token}`)

  let body = opcoes.body
  if (opcoes.json) {
    headers.set('Content-Type', 'application/json')
    body = JSON.stringify(opcoes.json)
  }

  const resposta = await fetch(caminho, { method: opcoes.method ?? 'GET', headers, body })
  if (resposta.status === 401) {
    sairPorAcesso()
    return null
  }

  return resposta
}

async function carregarUsuarios() {
  const resposta = await pedir('/auth/usuarios')
  if (!resposta?.ok) {
    if (resposta)
      mostrarAviso(await mensagem(resposta))
    return
  }

  const usuarios = await resposta.json()
  listaUsuarios.replaceChildren(...usuarios.map(linhaUsuario))
}

function linhaUsuario(usuario) {
  const linha = document.createElement('tr')
  linha.append(
    celula(usuario.login),
    celula(usuario.nome),
    celula(usuario.email),
    acoesUsuario(usuario),
  )
  return linha
}

function acoesUsuario(usuario) {
  const td = document.createElement('td')
  const alterar = document.createElement('button')
  alterar.type = 'button'
  alterar.className = 'secundario'
  alterar.textContent = 'Alterar'
  alterar.addEventListener('click', () => abrirAlteracao(usuario))
  td.append(alterar)

  if (usuario.login === LOGIN_ADM)
    return td

  const remover = document.createElement('button')
  remover.type = 'button'
  remover.className = 'secundario'
  remover.textContent = 'Remover'
  remover.addEventListener('click', () => removerUsuario(usuario))
  td.append(document.createTextNode(' '), remover)
  return td
}

async function criarUsuario(evento) {
  evento.preventDefault()
  limparAviso()
  const form = evento.currentTarget
  const dados = new FormData(form)
  const resposta = await pedir('/auth/usuarios', {
    method: 'POST',
    json: {
      login: dados.get('login'),
      senha: dados.get('senha'),
      nome: dados.get('nome'),
      email: dados.get('email'),
    },
  })
  if (!resposta)
    return
  if (!resposta.ok) {
    mostrarAviso(await mensagem(resposta))
    return
  }

  form.reset()
  await carregarUsuarios()
}

function abrirAlteracao(usuario) {
  usuarioEditado = usuario
  formAlterar.hidden = false
  formAlterar.elements.nome.value = usuario.nome
  formAlterar.elements.email.value = usuario.email
  formAlterar.elements.senha.value = ''
  document.querySelector('#alterar-login').textContent = `Login ${usuario.login}`
}

function fecharAlteracao() {
  usuarioEditado = null
  formAlterar.hidden = true
  formAlterar.reset()
}

async function salvarUsuario(evento) {
  evento.preventDefault()
  if (!usuarioEditado)
    return

  limparAviso()
  const form = evento.currentTarget
  const dados = new FormData(form)
  const resposta = await pedir(`/auth/usuarios/${usuarioEditado.id}`, {
    method: 'PUT',
    json: {
      nome: dados.get('nome'),
      email: dados.get('email'),
      senha: dados.get('senha'),
    },
  })
  if (!resposta)
    return
  if (!resposta.ok) {
    mostrarAviso(await mensagem(resposta))
    return
  }

  fecharAlteracao()
  await carregarUsuarios()
}

async function removerUsuario(usuario) {
  limparAviso()
  const resposta = await pedir(`/auth/usuarios/${usuario.id}`, { method: 'DELETE' })
  if (!resposta)
    return
  if (!resposta.ok) {
    mostrarAviso(await mensagem(resposta))
    return
  }

  if (usuarioEditado?.id === usuario.id)
    fecharAlteracao()
  await carregarUsuarios()
}

async function enviarVideo(evento) {
  evento.preventDefault()
  limparAviso()
  const form = evento.currentTarget
  const dados = new FormData(form)
  const botao = form.querySelector('#enviar')
  const andamento = document.querySelector('#envio-andamento')
  botao.disabled = true
  botao.textContent = 'Enviando…'
  andamento.hidden = false
  form.arquivo.disabled = true

  try {
    const resposta = await pedir('/video/videos', { method: 'POST', body: dados })
    if (!resposta)
      return
    if (!resposta.ok) {
      mostrarAviso(await mensagem(resposta))
      return
    }

    form.reset()
    await carregarVideos()
  } finally {
    botao.disabled = false
    botao.textContent = 'Enviar'
    andamento.hidden = true
    form.arquivo.disabled = false
  }
}

async function carregarVideos() {
  const resposta = await pedir('/video/videos')
  if (!resposta?.ok) {
    if (resposta)
      mostrarAviso(await mensagem(resposta))
    return
  }

  const videos = await resposta.json()
  listaVideos.replaceChildren(...videos.map(linhaVideo))
  vazio.hidden = videos.length > 0
  notaErro.hidden = !videos.some((video) => video.status === 'erro')
  agendarLista(videos)
}

function linhaVideo(video) {
  const linha = document.createElement('tr')
  const id = celula(video.id)
  id.className = 'celula-id'
  linha.append(id, celula(statusTexto[video.status] ?? video.status))

  const acao = document.createElement('td')
  if (video.status === 'concluido') {
    const baixar = document.createElement('button')
    baixar.type = 'button'
    baixar.textContent = 'Baixar ZIP'
    baixar.addEventListener('click', () => baixarZip(video.id))
    acao.append(baixar)
  }
  linha.append(acao)
  return linha
}

function agendarLista(videos) {
  pararLista()
  if (!videos.some((video) => ABERTOS.has(video.status)))
    return

  temporizador = window.setTimeout(() => carregarVideos(), 3000)
}

function pararLista() {
  if (temporizador !== null) {
    window.clearTimeout(temporizador)
    temporizador = null
  }
}

async function baixarZip(id) {
  limparAviso()
  const resposta = await pedir(`/video/videos/${id}/download`)
  if (!resposta)
    return
  if (!resposta.ok) {
    mostrarAviso(await mensagem(resposta))
    return
  }

  const blob = await resposta.blob()
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${id}.zip`
  link.click()
  URL.revokeObjectURL(url)
}

function loginDoToken(token) {
  try {
    const parte = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const preenchido = parte.padEnd(parte.length + ((4 - (parte.length % 4)) % 4), '=')
    const json = JSON.parse(atob(preenchido))
    return typeof json.login === 'string' && json.login ? json.login : null
  } catch {
    return null
  }
}

async function mensagem(resposta) {
  try {
    const corpo = await resposta.json()
    if (typeof corpo.erro === 'string' && corpo.erro)
      return corpo.erro
  } catch {
    // corpo vazio
  }

  return 'Não foi possível concluir a ação.'
}

function mostrarAviso(texto) {
  aviso.hidden = false
  aviso.textContent = texto
}

function limparAviso() {
  aviso.hidden = true
  aviso.textContent = ''
}

function celula(texto) {
  const td = document.createElement('td')
  td.textContent = texto ?? ''
  return td
}
