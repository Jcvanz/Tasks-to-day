# 📌 Tasks Today (Tasks.App)

<div align="center">

![Tasks Today Banner](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WPF UI](https://img.shields.io/badge/UI-WPF%20Fluent%203.0-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![SQLite](https://img.shields.io/badge/Database-SQLite%20EF%20Core-003B57?style=for-the-badge&logo=sqlite&logoColor=white)
![CommunityToolkit MVVM](https://img.shields.io/badge/MVVM-CommunityToolkit-68217A?style=for-the-badge&logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?style=for-the-badge&logo=windows11&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)

<p align="center">
  <strong>Um gerenciador de tarefas moderno, elegante e completo para desktop com interface Fluent Design (Windows 11).</strong>
  <br />
  Organize seu dia a dia, acompanhe hábitos recorrentes e gerencie seus grandes objetivos com facilidade.
</p>

</div>

---

## 🎯 Sobre o Projeto

O **Tasks Today** é uma aplicação desktop desenvolvida em **C# e .NET 8 (WPF)** que combina a simplicidade do acompanhamento diário de tarefas com o poder de um **quadro Kanban de objetivos**.

Com foco em produtividade, segurança e experiência de usuário de ponta, o aplicativo oferece suporte a múltiplos usuários, autenticação local segura com verificação por e-mail (SMTP), armazenamento offline com SQLite e uma interface visual moderna baseada no **Fluent Design System** da Microsoft.

---

## ✨ Principais Funcionalidades

### 🔐 1. Autenticação & Perfil de Usuário
- **Cadastro e Login:** Suporte a múltiplos usuários locais com senhas criptografadas (Hash + Salt).
- **Verificação em 2 Etapas / Código por E-mail:** Envio de código OTP via SMTP para validação de conta e recuperação.
- **Gerenciamento de Perfil:** Atualização de nome, telefone, avatar/foto de perfil e alteração de senha.
- **Sessão Persistente:** Opção de lembrar sessão ativa para inicialização rápida.

### 📅 2. Meu Dia a Dia (Daily Tasks)
- **Tarefas com Recorrência Inteligente:**
  - *Apenas neste dia*
  - *Próximos 3, 15 ou 30 dias*
  - *Dias Úteis (Segunda a Sexta)*
  - *Todos os dias*
- **Histórico por Data:** Navegue entre datas passadas e futuras para revisar ou planejar seu progresso.
- **Checklists Diários:** Subtarefas com progresso independente por data.
- **Prioridades Coloridas:** Classificação visual em *Baixa*, *Média*, *Alta* e *Urgente*.

### 📊 3. Quadro de Metas e Objetivos (Kanban)
- **Colunas Customizáveis:** Crie e personalize colunas de status com cores e ordens distintas.
- **Arrastar e Soltar (Drag & Drop):** Movimentação fluida de tarefas entre colunas usando `gong-wpf-dragdrop`.
- **Subtarefas (Checklist):** Barra de progresso visual de conclusão de cada item.
- **Data de Vencimento:** Acompanhamento de prazos para manter suas metas em dia.

### 🖥️ 4. Experiência de Usuário & Integração com o Sistema
- **Fluent UI / Windows 11 Style:** Componentes modernos com cantos arredondados, tipografia limpa e transições suaves.
- **Bandeja do Sistema (System Tray):** Minimiza para a bandeja com suporte a atalhos rápidos e notificações nativas.
- **Banco de Dados Local:** Dados salvos localmente e com alta performance através do **Entity Framework Core** com **SQLite**.

---

## 🛠️ Tecnologias e Bibliotecas

| Tecnologia / Pacote | Finalidade |
| :--- | :--- |
| **.NET 8 (WPF)** | Plataforma principal para aplicação desktop Windows |
| **WPF-UI (v3.0)** | Design system Fluent UI inspirado no Windows 11 |
| **CommunityToolkit.Mvvm** | Padrão arquitetural MVVM com source generators para alta performance |
| **EF Core + SQLite** | ORM e banco de dados relacional leve e embutido |
| **gong-wpf-dragdrop** | Drag and drop intuitivo para o quadro Kanban |
| **H.NotifyIcon.Wpf** | Integração nativa com a área de notificação do Windows (Tray Icon) |
| **Microsoft.Extensions.DependencyInjection** | Injeção de dependência e desacoplamento de serviços |

---

## 📁 Estrutura do Projeto

```text
Tasks-to-day/
├── src/
│   └── Tasks.App/
│       ├── Converters/        # Value Converters para binding XAML
│       ├── Data/              # DbContext e migrações do EF Core
│       ├── Models/            # Entidades de Domínio (User, DailyTask, TaskColumn, etc.)
│       ├── Resources/         # Ícones, estilos e assets visuais
│       ├── Services/          # Regras de negócio, Autenticação, SMTP e Notificações
│       ├── ViewModels/        # Camada MVVM (Auth, Kanban, DailyTasks, Main, etc.)
│       ├── Views/             # Janelas e Telas XAML (Fluent Windows e UserControls)
│       ├── App.xaml           # Inicialização e injeção de dependências
│       └── appsettings.json   # Configurações do app (SMTP, etc.)
├── .gitignore
├── README.md
└── Tasks.sln
```

---

## 🚀 Como Executar o Projeto

### Pré-requisitos
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.
- Visual Studio 2022 (com a carga de trabalho de desenvolvimento para desktop .NET) ou Visual Studio Code.
- Sistema Operacional: Windows 10 (versão 1809+) ou Windows 11.

### Passo a Passo

1. **Clone o repositório:**
   ```bash
   git clone https://github.com/Jcvanz/Tasks-to-day.git
   cd Tasks-to-day
   ```

2. **Restaure os pacotes NuGet:**
   ```bash
   dotnet restore
   ```

3. **Configure o SMTP (Opcional para envio real de e-mails):**
   Edite o arquivo `src/Tasks.App/appsettings.json` ou as variáveis de ambiente com as credenciais do seu provedor de e-mail (Gmail, Outlook, etc.):
   ```json
   {
     "SmtpSettings": {
       "Host": "smtp.gmail.com",
       "Port": 587,
       "EnableSsl": true,
       "UserName": "seu-email@gmail.com",
       "Password": "sua-senha-de-app",
       "SenderEmail": "seu-email@gmail.com",
       "SenderName": "Tasks Today"
     }
   }
   ```

4. **Execute a aplicação:**
   ```bash
   dotnet run --project src/Tasks.App/Tasks.App.csproj
   ```

---

## 🧭 Próximos Passos (Roadmap)

- [ ] Suporte a temas personalizados (Dark/Light dinâmico com sincronização com o sistema).
- [ ] Exportação e importação de dados de tarefas em JSON/CSV.
- [ ] Gráficos e estatísticas de produtividade semanal/mensal.
- [ ] Lembretes agendados com alarmes sonoros configuráveis.

---

## 📄 Licença

Este projeto está sob a licença MIT. Consulte o arquivo de licença para obter mais detalhes.

<div align="center">
  Feito com 💙 para aumentar sua produtividade diária!
</div>
