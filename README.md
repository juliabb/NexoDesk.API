# NexoDesk API

Backend de uma central corporativa de serviços internos. O NexoDesk permitirá
que colaboradores abram e acompanhem solicitações para áreas como TI, RH,
infraestrutura e compras.

## Tecnologias

- .NET 10 e ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Autenticação JWT
- Swagger/OpenAPI

## Funcionalidades atuais

- Cadastro, autenticação e encerramento de sessão
- Aprovação e administração de usuários
- Perfis de usuário, gestor e administrador
- Organização por coordenadorias e áreas
- Bloqueio após tentativas de login inválidas
- Troca obrigatória e histórico de senhas
- Proteção de CPF com HMAC
- Auditoria de alterações em usuários
- Cadastro e manutenção de categorias de chamados
- Abertura de chamados com protocolo automático
- Consulta paginada com filtros por status e prioridade
- Controle de acesso aos detalhes do chamado
- Fila de triagem por coordenadoria e área responsável
- Atribuição de chamados a usuários da área de atendimento
- Transições controladas de status
- Histórico de abertura, atribuições e mudanças de status
- Comentários públicos entre solicitante e equipe
- Comentários internos visíveis somente para a equipe de atendimento
- Perfil de atendente com acesso restrito à fila da própria área
- Notificações internas para atribuições, status e comentários públicos
- Caixa paginada, contador de não lidas e confirmação de leitura
- Anexos públicos e internos com download protegido por autorização
- Armazenamento local com limite de 10 MB e nomes físicos aleatórios

## Fluxo dos chamados

```text
Aberto → Em triagem → Em atendimento → Resolvido → Fechado
                            ↕
                  Aguardando solicitante
```

Chamados em atendimento também podem ser cancelados. Um chamado resolvido pode
voltar para atendimento, enquanto chamados fechados e cancelados são terminais.

Atendentes operam a fila da própria área, gestores operam as áreas de sua
coordenadoria e administradores podem operar todas as filas.

## Próxima etapa

Implementar indicadores de prazo e SLA do fluxo de atendimento.

## Deploy no Render

O repositório possui um `Dockerfile` na raiz. No Web Service, use Docker, contexto
`.` e caminho `./Dockerfile`. Configure `/healthz` como Health Check Path.

Variáveis obrigatórias: `DATABASE_URL`, `JWT_SECRET`, `JWT_ISSUER`,
`JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES`, `CPF_HMAC_KEY` e
`Database__ApplyMigrations=true`.

O armazenamento local de anexos é adequado apenas para desenvolvimento. Em
serviços sem disco persistente, os arquivos são perdidos após reinícios e deploys.
