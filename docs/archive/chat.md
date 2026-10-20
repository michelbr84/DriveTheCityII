
Loading older messages…
Gostei dessa ideia! Crie o roadmap completo de como devemos fazer isso desde agora ate o mesmo ficar completo
Sim. Eu estruturaria o projeto para chegar a um mod completo de “vida e carreira na cidade”, não apenas a um modo de dirigir. O caminho será construir primeiro um ciclo pequeno e confiável — caminhar, entrar no carro, trabalhar, estacionar e salvar — e depois ampliar esse ciclo com profissões, veículos, empresas e desafios.

Um ajuste importante em relação às respostas anteriores: caminhada, transições e integração de carreira são objetivos do novo mod, não funcionalidades que já validamos na sua instalação. O roadmap precisa transformar essas possibilidades em recursos comprovados, etapa por etapa.

Baixar o roadmap completo em Markdown — para usar como ROADMAP.md no projeto

Roadmap — Drive the City II
Nome provisório · Windows · Mod de Cities: Skylines II

1. Definição do produto final
A experiência que vamos buscar é:

Você constrói sua cidade normalmente. Depois, assume um personagem, caminha até um veículo, começa a trabalhar, ganha dinheiro, compra carros, desenvolve uma empresa e continua explorando a cidade que criou.

Cities: Skylines II será o mundo; o novo mod será a experiência jogável dentro dele.

O Beta Test Driving Mod será a base técnica a auditar e adaptar. Seu repositório declara licença MIT, controle de veículos existentes e câmera de perseguição, mas também informa que ainda não implementa colisões com edifícios. Portanto, não podemos tratar sua direção como uma base física já completa. 

Motor Town será referência de design: empregos de táxi, ônibus e carga, compra/aluguel de veículos e administração de empresas estão documentados na descrição oficial do jogo. Nós desenvolveremos sistemas próprios para reproduzir esses tipos de experiência dentro do CS2. 

O que deverá existir na versão 1.0
Área	Escopo proposto
Exploração	Personagem visível, caminhada, corrida e câmeras em primeira e terceira pessoa.
Veículos	Entrar, sair, dirigir, estacionar e controlar diferentes classes de veículos.
Profissões	Táxi, entregas urbanas, transporte de carga e motorista de ônibus.
Progressão	Dinheiro, experiência, reputação, desbloqueios e propriedade de veículos.
Garagem	Compra, aluguel, venda, combustível e manutenção simplificada.
Vida urbana	Base residencial, pontos de serviço e interações simples com personagens de missão.
Longo prazo	Empresa de transporte básica, frota gerencial e contratos.
Diversão adicional	Contrarrelógio, checkpoints e desafios de estacionamento/condução.
Qualidade	Salvamento, recuperação, tutorial, gamepad, interface PT-BR/EN e instalação documentada.
Não colocaria como requisito da primeira versão interiores em todos os prédios, desmontagem mecânica completa, deformação estrutural de carros, perseguições policiais complexas ou multiplayer. Essas possibilidades entram em expansões, sem impedir que a versão inicial seja um produto completo.

2. As decisões de arquitetura que tomaremos desde o início
O CS2 continuará sendo o único jogo executando o mundo
Não exportaremos a cidade para outro motor e não tentaremos manter duas simulações completas sincronizadas.

A meta é que o pacote principal precise apenas de Cities: Skylines II e das dependências documentadas do mod. Motor Town não precisará estar aberto para executar sistemas que escrevermos por conta própria.

Um único sistema controlará o jogador por vez
O projeto terá estados explícitos:

Construção ↔ A pé ↔ Entrando no veículo ↔ Dirigindo ↔ Saindo do veículo.

Também haverá estados de pausa, carregamento e recuperação.

A IA do jogo e o controlador do jogador não poderão disputar a movimentação do mesmo veículo. Da mesma forma, não deixaremos a câmera da construção e a câmera do personagem disputarem o enquadramento.

A economia pessoal começará separada da economia municipal
No início, missões terão dinheiro, passageiros e cargas pertencentes à carreira do mod.

A cidade fornecerá os lugares e a infraestrutura. Alterar mercadorias reais, passageiros nativos ou contas municipais será uma integração posterior.

Essa separação evita que um erro na primeira missão de táxi comprometa a simulação inteira.

Salvamento será parte da fundação
Não deixaremos persistência para o fim. Desde o primeiro esqueleto, precisaremos definir como relacionar personagem, dinheiro, veículos e missões à cidade e à versão correta do save.

3. Desenvolvimento por fases
Fase 0 — Auditar o ambiente Windows e comprovar a base
Objetivo: descobrir exatamente o que funciona antes de ampliar o projeto.

Registrar a versão instalada do CS2, loja, DLCs, diretórios, ferramenta de modding e mods ativos. Preparar uma configuração isolada de testes, cópias dos saves e uma cidade-laboratório descartável.

Depois, selecionar um commit do Beta Test Driving Mod, conferir sua licença e dependências, compilar o original e testar direção e devolução do veículo à IA.

A toolchain e os templates oficiais são o ponto de partida documentado para desenvolvimento de mods. As versões de Unity e demais dependências devem acompanhar o ambiente compatível com o jogo, não simplesmente as versões mais recentes disponíveis. 

Uma particularidade que já verifiquei: o projeto-base consultado usa net48. Portanto, não devemos migrá-lo automaticamente para outro .NET apenas porque alguma ferramenta auxiliar exige um SDK mais novo. 

Também estudaremos o ciclo de Motor Town: aceitar trabalho, coletar ou embarcar, transportar, entregar, receber e investir. O resultado será uma especificação própria, não uma tentativa de importar seu código.

Critério de aprovação: build reproduzível, versão/commit registrados, execução documentada e lista clara de limitações.

Se a base falhar: corrigir ou substituir pontualmente o que falhou antes de acrescentar profissões.

Fase 1 — Criar o novo mod e proteger os saves
Objetivo: ter um projeto independente, organizado e reversível.

Criar identidade própria para o mod, mantendo os créditos do código reutilizado. O novo pacote e o Beta Test Driving original não deverão carregar simultaneamente os mesmos controladores.

Separar a implementação em módulos de integração com a cidade, personagem, veículos, missões, economia, persistência e interface.

Desde aqui, implementar configuração, logs, diagnóstico, identificação de versão e desativação segura.

A decisão mais importante desta fase: como salvar
Dar preferência a dados associados ao próprio save, caso os mecanismos disponíveis permitam. Se precisarmos de arquivo auxiliar, ele deverá estar vinculado à identidade e à revisão exatas daquele save.

Não basta usar o nome da cidade:

Carregar um save antigo não pode trazer o dinheiro de uma sessão futura.

Também não presumiremos que uma referência temporária de entidade continue válida após recarregar. Veículos, edifícios e destinos precisarão ser revalidados.

Critério de aprovação: instalar, abrir, salvar, fechar, reabrir e desativar o esqueleto sem alterações indevidas na cidade. Dados inválidos não podem sobrescrever a única cópia válida.

Fase 2 — Fazer a direção básica funcionar corretamente
Objetivo: assumir um veículo sem brigar com a IA.

Começar com um único automóvel e implementar aceleração, frenagem, ré, esterçamento progressivo, freio de estacionamento, velocímetro e câmera de perseguição.

As teclas da direção não deverão movimentar simultaneamente a câmera de construção ou ativar ferramentas urbanas.

A liberação do carro merece tanta atenção quanto a condução: devolver o controle, reconstruir a navegação necessária e limpar referências temporárias nas faixas.

Separar modo de testes de modo carreira
Assumir qualquer veículo existente poderá servir ao sandbox de desenvolvimento.

Na carreira, o jogador deverá usar veículos próprios, alugados ou disponibilizados por um serviço. Não transformaremos automaticamente o carro de um cidadão em propriedade do jogador.

Critério de aprovação: dirigir por curvas, cruzamentos e subidas, devolver o veículo e verificar que ele não retorna indevidamente à rota antiga nem deixa bloqueios invisíveis no trânsito.

Fase 3 — Resolver superfícies, colisões e física
Objetivo: não confundir movimentação visual convincente com física confiável.

Criar uma pista-laboratório contendo rua plana, calçada, rampa, ponte, túnel, vias sobrepostas e edifícios.

Precisaremos verificar quais superfícies podem ser consultadas e quais colisões realmente existem. Não vamos presumir que uma rua renderizada já seja uma superfície pronta para um controlador de veículos convencional.

Quando necessário, estudar volumes de colisão simplificados na região próxima ao jogador, com limites de memória e descarte.

Escolher uma abordagem física
Comparar, em um único carro:

Controle cinemático aprimorado: movimentação calculada pelo mod, com consultas de superfície, aderência e colisão.

Controlador físico local: uma implementação mais dinâmica, caso a integração com terreno, renderização e tráfego seja comprovada.

Escolheremos uma autoridade de movimento; não aplicaremos duas físicas simultaneamente.

A meta da v1.0 será direção semi-realista, consistente e agradável, não prometer física estrutural equivalente a um simulador automotivo especializado.

Definir os relógios do jogo
Minha proposta inicial é usar velocidade normal durante a exploração, congelar movimento e temporizadores relevantes quando houver pausa e restaurar a velocidade anterior ao retornar à construção.

Perder foco deve limpar as entradas. Quedas de FPS não podem acumular um deslocamento enorme para o quadro seguinte.

Critério de aprovação: não atravessar edifícios, não trocar de pavimento em pontes/túneis e manter comportamento previsível após pausa, perda de foco e variações de FPS.

Fase 4 — Implementar o personagem a pé
Objetivo: controlar um personagem real, não apenas uma câmera voadora.

Criar um avatar pertencente ao mod. Inicialmente, evitar assumir um cidadão que já tenha família, trabalho e agenda mantidos pela simulação.

Sua representação visual poderá usar recursos próprios autorizados ou uma representação nativa em execução, conforme a viabilidade do carregamento e das animações.

Implementar caminhada, corrida, rotação, visão livre, pequenos desníveis e alternância entre primeira e terceira pessoa.

Aproveitar a infraestrutura de superfícies da fase anterior, adaptando-a às dimensões e ao movimento do personagem. Acrescentar câmera anticolisão, animações básicas e recuperação para um ponto seguro.

Critério de aprovação: percorrer um circuito urbano com calçadas, rampas, ponte e obstáculos sem atravessar fachadas, cair no terreno ou perder permanentemente o controle.

Fora deste marco: parkour, natação e interiores genéricos.

Fase 5 — Unir caminhada e direção
Objetivo: entrar no carro, dirigir, estacionar e continuar a pé.

Implementar seleção por proximidade, indicação de interação, verificação de disponibilidade e transição de controle/câmera.

Inicialmente, entrada e saída ocorrerão com o veículo parado. As transições devem acontecer no mesmo cenário, sem depender de carregar outro jogo.

O ponto delicado: onde o personagem aparece ao sair
Precisaremos verificar espaço livre, altura, parede, outros carros e lado da calçada.

Quando um lado estiver bloqueado, tentar uma saída alternativa. Se não existir posição segura, informar o problema ou acionar recuperação — nunca colocar o personagem dentro de uma parede para concluir a ação.

Também distinguir estacionamento de veículo próprio, devolução de veículo temporariamente assumido e remoção de entidades criadas pelo mod.

Critério de aprovação: 50 ciclos de entrar, dirigir e sair nos cenários de teste, sem duplicações, perda de câmera ou bloqueio de tráfego.

Marco M1 — Sandbox jogável
Aqui já será possível:

Construir → caminhar → entrar → dirigir → estacionar → sair → voltar à construção.

Ainda não será a carreira completa, mas sua fundação estará demonstrada.

Fase 6 — Criar a primeira carreira completa: táxi
Objetivo: validar o produto inteiro em pequena escala.

O primeiro ciclo será:

Começar a pé, alugar um carro, aceitar uma corrida, buscar o passageiro, levá-lo ao destino, receber dinheiro e experiência, estacionar, sair e salvar.

As origens e os destinos deverão usar locais acessíveis da cidade. O ponto de coleta não pode ser simplesmente o centro geométrico de um prédio.

Antes de oferecer uma corrida, verificar conectividade e acesso.

Passageiro de missão primeiro; cidadão real depois
Na primeira implementação, usaremos passageiros próprios da carreira, com embarque e desembarque representados.

Isso não será anunciado como transporte de cidadãos reais da simulação. Assumir passageiros nativos será uma integração separada.

A missão precisará de estados claros de aceitação, coleta, transporte, conclusão, cancelamento e falha.

A recompensa deverá acontecer uma única vez, inclusive quando houver carregamento ou repetição de eventos.

Critério de aprovação: concluir 20 corridas em bairros distintos, incluindo cancelamento e save/load durante o serviço.

Marco M2 — MVP de carreira
Aqui já teremos um pequeno jogo completo, repetível e demonstrável. Esse será o momento de avaliar se caminhar e dirigir são realmente divertidos antes de multiplicar conteúdo.

Fase 7 — Adicionar progressão, garagem e manutenção
Objetivo: fazer o trabalho gerar escolhas e objetivos.

Implementar saldo, histórico de transações, experiência por profissão, reputação, desbloqueios e tutorial econômico.

Adicionar aluguel, compra, venda e veículos próprios associados ao perfil daquela cidade.

Na garagem, começar com combustível, desgaste e manutenção simplificada. Os serviços poderão ser interações em pontos externos escolhidos pelo jogador, sem exigir que toda cidade possua um prédio específico de oficina.

Os upgrades precisam produzir efeitos verificáveis, como frenagem melhor, capacidade maior ou comportamento diferente dos pneus.

Evitar bloqueios permanentes
Um jogador sem dinheiro ou com veículo imobilizado precisa de uma saída: trabalho inicial acessível, aluguel básico ou recuperação claramente explicada.

Nenhuma peça obrigatória indisponível poderá impedir para sempre que o jogador volte a dirigir.

Critério de aprovação: um perfil novo consegue alugar, trabalhar, comprar e manter um veículo, com todas as transações persistindo corretamente.

Fase 8 — Expandir para entregas, caminhões e ônibus
Objetivo: reutilizar a infraestrutura de missões em profissões diferentes.

Entregas urbanas: vários destinos, coleta, prazo, fragilidade e avaliação da condução.

Caminhão rígido: capacidade, massa da carga, manobras e pontos industriais/comerciais de carga e descarga.

Ônibus: sequência de paradas, capacidade, embarque/desembarque e avaliação da viagem.

Ônibus terá dois níveis de integração
O primeiro usará linhas e paradas existentes na cidade, mas com passageiros da carreira.

O segundo tentará integrar passageiros, tarifas e agendamento nativos.

A primeira modalidade atende ao escopo proposto da v1.0. A segunda só será anunciada depois de validada.

Caminhões articulados também terão protótipo próprio: não serão tratados como caminhões rígidos com uma aparência diferente.

Critério de aprovação: cada profissão possui contratos alcançáveis, tutorial, conclusão, falha e recompensa. Demolir um destino ou alterar uma linha deve gerar replanejamento ou cancelamento seguro.

Fase 9 — Criar base residencial, serviços e empresa
Objetivo: dar identidade e progressão de longo prazo ao personagem.

Permitir escolher uma base residencial e uma garagem associadas a edifícios da cidade, com interação externa e retorno seguro.

Isso não pressupõe um interior navegável em cada construção.

Adicionar personagens de serviço para receber trabalho, alugar veículo, reparar e administrar contratos. Não tentaremos criar uma simulação social completa para todos os moradores.

Empresa de transporte
Implementar nome, contratos, frota, contratação, salários e custos.

Na primeira versão, funcionários poderão funcionar como simulação gerencial explicitamente identificada. Não alegaremos que seus carros estão circulando fisicamente se o sistema apenas calcular serviços.

A frota física será uma integração posterior.

Critério de aprovação: progredir de autônomo a proprietário de uma pequena empresa, comprar um segundo veículo, contratar um motorista e recarregar o save sem inconsistências financeiras.

Fase 10 — Fazer a cidade influenciar a carreira
Objetivo: aproveitar o principal diferencial do projeto: trabalhar na cidade que você construiu.

Adicionar contrarrelógio, desafios de estacionamento e percursos por checkpoints.

Usar tipos de edifícios, acessibilidade, linhas e distâncias de rota para orientar contratos. A infraestrutura construída deverá afetar os trajetos que o jogador realmente percorre.

Também tratar alterações urbanas: via demolida, acesso removido, linha modificada ou destino indisponível.

Minha proposta é permitir edição no modo construção e revalidar personagem, carro e missões quando o jogador voltar à carreira.

Integração econômica profunda ficará separada
Ler informações da cidade não é o mesmo que alterar a simulação.

Uma entrega do mod não deverá satisfazer uma demanda nativa enquanto o jogo também mantém outra entrega para o mesmo pedido sem reconciliação.

A escrita na economia municipal poderá começar como experimento opcional, desligado por padrão, com um único tipo de operação.

Critério de aprovação: modificar a cidade durante uma carreira não corrompe perfil ou save e não mantém missões impossíveis indefinidamente.

Fase 11 — Refinar apresentação, interface e acessibilidade
Objetivo: transformar os sistemas em uma experiência coesa.

Consolidar HUD, objetivos, rota/mapa, carteira, garagem, empresas, configurações e tutorial.

Adicionar remapeamento, gamepad, escalonamento de interface, textos PT-BR/EN e legendas.

Refinar animações, transições, passos, motor, pneus e impactos usando recursos próprios ou autorizados.

Avaliar o cenário ao nível da rua: iluminação, proximidade da câmera, recortes e detalhe. Melhorias visuais devem ser opcionais e medidas, sem aumentar indiscriminadamente o detalhe da cidade inteira.

Câmera interna não significa cockpit pronto
Cockpit detalhado ficará restrito aos veículos que realmente tenham interior adequado.

Nos demais, oferecer câmera de capô ou posição de direção com limitações claras, sem anunciar uma cabine completa inexistente.

Critério de aprovação: um jogador novo consegue iniciar a exploração, alugar um carro e concluir seu primeiro trabalho sem console nem ajuda do desenvolvedor.

Fase 12 — Executar a beta e os testes de lançamento
Objetivo: provar que funciona além da cidade-laboratório.

Testar cidades pequena, média e grande, bairros densos, pontes, túneis, trânsito pesado, diferentes DLCs e combinações de mods declaradas como suportadas.

A avaliação terá três camadas:

Camada	O que deverá comprovar
Testes de lógica	Pagamentos, progressão, contratos, propriedade e migração de dados.
Testes de integração	Transições de controle, entidades, câmera e persistência.
Testes dentro do jogo	Dirigibilidade, colisões, tráfego, interface e diversão.
Compilar com sucesso não substitui testar o jogo.

Como critérios iniciais de qualidade, proponho 30 ciclos de save/load, sessões prolongadas de pelo menos duas horas e testes de atualização, desativação, dados antigos e falha de gravação.

Mediremos tempo de quadro, memória e velocidade da simulação nas mesmas condições. Uma regressão recorrente superior a 10% no tempo de quadro pode servir como alerta inicial de investigação, não como promessa universal de desempenho.

Critério de aprovação: nenhum problema crítico conhecido de corrupção de save, dinheiro duplicado, perda permanente de controle ou impossibilidade de concluir conteúdo obrigatório.

Fase 13 — Publicar a versão 1.0 e estabelecer manutenção
Objetivo: permitir que outra pessoa instale e jogue sem ferramentas de desenvolvimento.

Congelar novos recursos, resolver bloqueadores e fechar a matriz de compatibilidade.

Preparar pacote, dependências mínimas, changelog, créditos, guia rápido, problemas conhecidos e procedimento de migração.

Também documentar uma saída limpa: encerrar controle, estacionar ou devolver entidades, salvar em uma nova cópia e verificar a abertura sem o mod.

Não prometer desinstalação inofensiva antes de testar esse procedimento.

Distribuição
Paradox Mods será o canal principal a validar; a toolchain oficial documenta o fluxo de publicação. Testaremos também num ambiente sem SDKs para confirmar que o jogador não precisa das ferramentas usadas para desenvolver. 

Melty será um canal adicional. Seu site descreve um fluxo Windows com Claude Code/Codex, mas isso não comprova a compatibilidade deste projeto específico. Instalação, lançamento e remoção deverão passar por testes próprios. 

Preservaremos os avisos do código MIT reutilizado. A licença do mod-base não concede direitos sobre marcas, arquivos do jogo ou outros conteúdos de terceiros. 

Critério de aprovação: um terceiro instala numa configuração suportada, percorre o ciclo completo e encontra instruções claras de atualização e recuperação.

4. Como organizar o trabalho do Claude Code
Eu não começaria com uma ordem para implementar tudo simultaneamente.

Cada lote deverá seguir:

Entender o estado → implementar um objetivo → compilar → testar → registrar evidências → revisar → atualizar o roadmap.

Um segundo agente poderá revisar mudanças isoladas, mas não deixaria vários agentes alterarem simultaneamente o controlador de movimento ou o formato dos saves.

O pacote comunitário cs2-modding pode fornecer referências; coherent-gameface oferece ferramentas de inspeção de interface em ambientes compatíveis. Já unity-devtools exige um Unity Mono dev build com depuração apropriada — não devemos presumir conexão automática com qualquer instalação comercial. 

Documentação essencial
Manter README.md, ROADMAP.md, PRD.md, ARCHITECTURE.md, AGENTS.md/CLAUDE.md, SAVE_FORMAT.md, COMPATIBILITY.md, QA_CHECKLIST.md, KNOWN_ISSUES.md, CHANGELOG.md e THIRD_PARTY_NOTICES.md.

Cada fase concluída precisa registrar commit, versão do jogo, cenário testado, resultado e evidência.

Quando faltar execução real, o status será “não testado”, não “concluído porque compilou”.

5. Marcos do projeto
Marco	Fases	Resultado esperado
M0 — Fundação	0–1	Ambiente seguro, base auditada, build e persistência.
M1 — Sandbox	2–5	Caminhada e direção conectadas, com controle confiável.
M2 — MVP	6	Primeira carreira completa e persistente.
M3 — Alfa de conteúdo	7–9	Progressão, quatro carreiras, garagem e empresa básica.
M4 — Beta	10–12	Cidade dinâmica, desafios, apresentação e validação ampla.
M5 — Versão 1.0	13	Produto distribuível, documentado e com suporte definido.
O avanço dependerá dos critérios de aprovação. Interface e lógica pura de carreira poderão avançar em paralelo, mas a integração ao mundo precisará esperar a estabilidade de controle e persistência.

6. O que virá depois da versão completa
Serviços urbanos: passageiros nativos, reboque, ambulância, bombeiros e missões policiais, cada um com sua própria validação.

Mais imersão: interiores próprios para garagem e residência, concessionária, animações adicionais, cockpits específicos e suporte avançado a volantes.

Logística avançada: caminhões articulados, carga física, funcionários circulando e integração correta com a economia municipal.

Sistemas maiores: manutenção peça a peça, danos estruturais e multiplayer. Este último exigirá um estudo separado de sincronização e autoridade da simulação; não será presumido como algo fornecido automaticamente pelo launcher.

Nossa primeira meta concreta
Começar pelas fases 0 e 1 e depois perseguir esta demonstração integrada:

Caminhar → entrar no carro → dirigir → concluir uma corrida → estacionar → sair → salvar → fechar → reabrir e continuar.

Quando esse ciclo estiver confiável, teremos a base do produto. A partir daí, cada nova profissão e sistema será uma expansão de uma experiência que já funciona — e não mais uma funcionalidade apoiada em um protótipo instável.


ROADMAP_Drive_the_City_II.md
Document
Como ficaria a estrutura de pastas completo dos documentos antes de criarmos prompts para os agentes como Claude code
Eu organizaria o Drive the City II com uma raiz pequena e uma pasta docs/ dividida por responsabilidade: produto, pesquisa, arquitetura, jogabilidade, planejamento, testes e operação dos agentes.

A regra mais importante seria: cada informação deve ter um único lugar oficial. O agente não deve encontrar três versões diferentes do escopo, nem interpretar um recurso planejado como algo que já funciona.

A estrutura abaixo preserva as fases 00 a 13 e os marcos do roadmap, mas transforma esse planejamento em documentos que poderão orientar tarefas específicas. 

Também preparei a proposta em arquivo: 
baixar a estrutura documental completa em Markdown
.

1. Estrutura completa proposta
Usaria DriveTheCityII como nome provisório da pasta. Os nomes dos arquivos ficariam em inglês, sem espaços; o conteúdo inicial pode permanecer em português.

Esta é a estrutura pretendida para todo o desenvolvimento, não uma recomendação para criar todos os arquivos vazios agora. Relatórios de auditoria, resultados de testes e instruções de instalação precisam ser preenchidos com informações verificadas conforme o trabalho avançar.

DriveTheCityII/
├── README.md
├── ROADMAP.md
├── AGENTS.md
├── CLAUDE.md
├── CHANGELOG.md
├── CONTRIBUTING.md
├── SECURITY.md
├── THIRD_PARTY_NOTICES.md
│
└── docs/
    ├── INDEX.md
    ├── PROJECT_STATUS.md
    ├── DOCUMENTATION_RULES.md
    │
    ├── product/
    │   ├── VISION.md
    │   ├── PRD.md
    │   ├── GAME_DESIGN.md
    │   ├── PLAYER_JOURNEYS.md
    │   └── EXPANSIONS.md
    │
    ├── research/
    │   ├── SOURCES.md
    │   ├── FEASIBILITY_MATRIX.md
    │   ├── BASE_MOD_AUDIT.md
    │   ├── MOTOR_TOWN_REFERENCE.md
    │   ├── CS2_INTEGRATION_FINDINGS.md
    │   └── MELTY_EVALUATION.md
    │
    ├── engineering/
    │   ├── WINDOWS_SETUP.md
    │   ├── TOOLCHAIN_MATRIX.md
    │   ├── LOCAL_ENVIRONMENT.example.md
    │   ├── BUILD_AND_RUN.md
    │   ├── CODING_STANDARDS.md
    │   ├── DEPENDENCIES.md
    │   ├── UPSTREAM_STRATEGY.md
    │   └── DEBUGGING.md
    │
    ├── architecture/
    │   ├── ARCHITECTURE.md
    │   ├── MODULE_CONTRACTS.md
    │   ├── CONTROL_AUTHORITY.md
    │   ├── ENTITY_LIFECYCLE.md
    │   ├── GAME_TIME_AND_PAUSE.md
    │   ├── WORLD_CONTACT.md
    │   ├── CITY_INTEGRATION.md
    │   ├── SAVE_FORMAT.md
    │   ├── ERROR_RECOVERY.md
    │   ├── PERFORMANCE_BUDGET.md
    │   └── decisions/
    │       ├── README.md
    │       ├── ADR-0001-host-game-and-runtime.md
    │       ├── ADR-0002-control-authority.md
    │       ├── ADR-0003-career-save-isolation.md
    │       └── ADR-0004-physics-strategy.md
    │
    ├── gameplay/
    │   ├── PLAYER_AND_CAMERAS.md
    │   ├── VEHICLE_DRIVING.md
    │   ├── WALK_DRIVE_TRANSITIONS.md
    │   ├── MISSION_FRAMEWORK.md
    │   ├── PROGRESSION_AND_ECONOMY.md
    │   ├── VEHICLE_OWNERSHIP_AND_GARAGE.md
    │   ├── HOME_AND_SERVICE_POINTS.md
    │   ├── COMPANIES_AND_FLEET.md
    │   ├── CHALLENGES_AND_DYNAMIC_CITY.md
    │   └── careers/
    │       ├── TAXI.md
    │       ├── DELIVERIES.md
    │       ├── FREIGHT.md
    │       └── BUS.md
    │
    ├── experience/
    │   ├── UI_AND_HUD.md
    │   ├── CONTROLS_AND_ACCESSIBILITY.md
    │   ├── ART_AND_ANIMATION.md
    │   ├── AUDIO.md
    │   ├── LOCALIZATION.md
    │   └── ONBOARDING.md
    │
    ├── planning/
    │   ├── BACKLOG.md
    │   ├── RISKS_AND_OPEN_QUESTIONS.md
    │   ├── TRACEABILITY.md
    │   ├── phases/
    │   │   ├── README.md
    │   │   ├── PHASE-00-audit-and-windows.md
    │   │   ├── PHASE-01-foundation-and-save-safety.md
    │   │   ├── PHASE-02-basic-driving.md
    │   │   ├── PHASE-03-surfaces-collisions-and-physics.md
    │   │   ├── PHASE-04-on-foot-character.md
    │   │   ├── PHASE-05-walk-drive-integration.md
    │   │   ├── PHASE-06-taxi-career-mvp.md
    │   │   ├── PHASE-07-progression-and-garage.md
    │   │   ├── PHASE-08-delivery-freight-and-bus.md
    │   │   ├── PHASE-09-home-services-and-company.md
    │   │   ├── PHASE-10-challenges-and-dynamic-city.md
    │   │   ├── PHASE-11-presentation-and-accessibility.md
    │   │   ├── PHASE-12-beta-and-validation.md
    │   │   └── PHASE-13-release-and-maintenance.md
    │   └── tasks/
    │       └── README.md
    │
    ├── quality/
    │   ├── TEST_STRATEGY.md
    │   ├── QA_CHECKLIST.md
    │   ├── TEST_SCENARIOS.md
    │   ├── SAVE_SAFETY_TESTS.md
    │   ├── BENCHMARK_PROTOCOL.md
    │   ├── COMPATIBILITY_MATRIX.md
    │   ├── KNOWN_ISSUES.md
    │   └── evidence/
    │       └── README.md
    │
    ├── agents/
    │   ├── READING_GUIDE.md
    │   ├── ROLES_AND_OWNERSHIP.md
    │   ├── WORKFLOW_AND_HANDOFF.md
    │   ├── DEFINITION_OF_READY.md
    │   ├── DEFINITION_OF_DONE.md
    │   └── TOOLS_AND_PERMISSIONS.md
    │
    ├── release/
    │   ├── PACKAGING_AND_PUBLISHING.md
    │   ├── INSTALL_UPGRADE_UNINSTALL.md
    │   ├── COMPATIBILITY_POLICY.md
    │   └── POST_RELEASE_MAINTENANCE.md
    │
    ├── legal/
    │   ├── REUSE_AND_LICENSE_AUDIT.md
    │   └── ASSET_PROVENANCE.md
    │
    ├── user/
    │   ├── QUICK_START.md
    │   └── TROUBLESHOOTING.md
    │
    ├── templates/
    │   ├── DOCUMENT_TEMPLATE.md
    │   ├── PHASE_TEMPLATE.md
    │   ├── TASK_TEMPLATE.md
    │   ├── ADR_TEMPLATE.md
    │   ├── TEST_REPORT_TEMPLATE.md
    │   └── HANDOFF_TEMPLATE.md
    │
    └── archive/
        └── README.md
O arquivo LICENSE entraria após definirmos a licença do novo projeto com base na auditoria. Avisos e licenças de componentes efetivamente reutilizados deverão ser preservados desde sua incorporação.

2. O papel dos documentos principais
Na raiz: orientação, regras e histórico
Documento	Responsabilidade
README.md	Explicar o projeto, apresentar seu estado e indicar por onde começar.
ROADMAP.md	Manter a sequência oficial das fases, dependências e marcos.
AGENTS.md	Definir as regras comuns de trabalho para os agentes.
CLAUDE.md	Servir como entrada dedicada ao Claude Code, referenciando as regras comuns e os caminhos de leitura.
CHANGELOG.md	Registrar mudanças realmente realizadas, não funcionalidades prometidas.
CONTRIBUTING.md	Definir contribuição, branches de trabalho, revisão e integração à principal.
SECURITY.md	Estabelecer proteção de dados, tratamento de problemas e limites de operações sensíveis.
THIRD_PARTY_NOTICES.md	Reunir atribuições dos componentes efetivamente incorporados.
Eu não colocaria o projeto inteiro dentro de CLAUDE.md. Ele deve encaminhar o agente aos documentos corretos, não duplicar o PRD, o roadmap e todas as especificações.

Também consolidaria o roadmap que já produzimos em ROADMAP.md. Não manteria dois roadmaps ativos sendo atualizados separadamente.

No início de docs/: mapa e estado real
INDEX.md será a porta de entrada da documentação: finalidade de cada documento, estado e momento de leitura.

PROJECT_STATUS.md responderá: em qual fase estamos, qual foi o último marco aceito, qual tarefa está ativa, quais são os bloqueios e onde estão as evidências.

DOCUMENTATION_RULES.md definirá nomenclatura, estados dos documentos, revisão e resolução de contradições.

O estado inicial precisa ser honesto: planejamento, sem compilação ou execução local comprovada do novo mod, conforme registrado no roadmap. 

3. Separar produto, pesquisa e implementação
product/: o que o jogo precisa entregar
O PRD.md será o documento oficial de requisitos: escopo obrigatório da v1.0, funcionalidades excluídas, requisitos de qualidade e critérios gerais de sucesso.

GAME_DESIGN.md explicará o ciclo de jogo e as decisões de design. PLAYER_JOURNEYS.md descreverá experiências completas, como:

Abrir a cidade → assumir o personagem → alugar um carro → trabalhar → receber → estacionar → salvar → continuar depois.

EXPANSIONS.md impedirá que ideias posteriores sejam confundidas com requisitos de lançamento. Interiores gerais, frota física e integração econômica profunda, por exemplo, não devem entrar silenciosamente na primeira versão.

research/: o que sabemos e o que ainda precisamos verificar
Aqui manteremos separadas as informações externas e os resultados da nossa investigação.

FEASIBILITY_MATRIX.md será especialmente importante. Um recurso poderá estar documentado por outro projeto, mas ainda não compilado ou testado por nós.

BASE_MOD_AUDIT.md registrará o commit avaliado, as dependências, os pontos de extensão, as limitações e as permissões de reutilização.

MOTOR_TOWN_REFERENCE.md descreverá os ciclos observáveis que queremos adaptar com implementação própria. MELTY_EVALUATION.md registrará sua utilidade e compatibilidade verificadas.

Isso preserva os papéis definidos no roadmap: CS2 como mundo, mod-base como candidato técnico, Motor Town como referência e Melty como ferramenta complementar. 

engineering/: como reproduzir o ambiente Windows
Essa pasta documentará instalação, ferramentas, compilação, execução e diagnóstico.

A TOOLCHAIN_MATRIX.md será o lugar oficial das versões confirmadas. Os outros documentos a referenciarão, em vez de repetir versões potencialmente divergentes.

LOCAL_ENVIRONMENT.example.md terá apenas campos e exemplos neutros. Caminhos reais da sua máquina ficarão em uma cópia local ignorada pelo Git.

Não preencheremos versões, APIs ou comandos por suposição só para deixar a documentação aparentemente completa.

4. Os documentos técnicos mais importantes
Dentro de architecture/, eu daria atenção especial a estes arquivos:

Documento	Pergunta que precisa responder
CONTROL_AUTHORITY.md	Quem pode movimentar personagem/veículo e controlar a câmera em cada estado?
ENTITY_LIFECYCLE.md	Como criar, assumir, devolver, invalidar e remover entidades com segurança?
GAME_TIME_AND_PAUSE.md	O que acontece com movimento, missões e economia ao pausar ou voltar à construção?
WORLD_CONTACT.md	Como identificar a superfície correta em ruas, calçadas, pontes, túneis e vias sobrepostas?
CITY_INTEGRATION.md	O que o mod apenas lê e o que pode alterar na simulação?
SAVE_FORMAT.md	Como identificar cidade, perfil e revisão do save, além de migrar e recuperar dados?
ERROR_RECOVERY.md	Como continuar com segurança quando um veículo, destino ou estado deixa de ser válido?
Esses arquivos detalham as proteções já previstas no roadmap para controle, integração urbana e persistência. 

As decisões precisam ter histórico
A pasta decisions/ conterá ADRs — registros de decisões de arquitetura.

Por exemplo, ADR-0004-physics-strategy.md deverá registrar as alternativas de física, os protótipos executados, os resultados, a escolha e suas consequências.

O nome aparecer na árvore não significa que a decisão já está tomada. A estratégia física continuará em aberto até existir evidência suficiente.

Quando uma decisão for aceita, as especificações afetadas deverão ser atualizadas. Não basta deixar documentos contraditórios e esperar que o agente escolha corretamente.

5. Como separar jogabilidade de apresentação
A pasta gameplay/ descreverá o comportamento do jogo. A pasta experience/ descreverá como o jogador percebe e controla esse comportamento.

Um exemplo:

Situação: sair de um carro estacionado junto a uma parede	Documento responsável
Verificar condições e procurar uma saída válida.	WALK_DRIVE_TRANSITIONS.md
Transferir controle do carro para o personagem.	CONTROL_AUTHORITY.md
Informar que a saída está bloqueada.	UI_AND_HUD.md
Definir botão, remapeamento e acessibilidade da ação.	CONTROLS_AND_ACCESSIBILITY.md
Definir a animação ou transição visual.	ART_AND_ANIMATION.md
Os documentos se referenciam, mas não repetem integralmente as mesmas regras.

Cada carreira terá uma especificação própria. TAXI.md, por exemplo, deverá cobrir pré-requisitos, geração de corridas, pontos acessíveis, embarque, destino, recompensa, cancelamento, persistência e testes.

Já MISSION_FRAMEWORK.md reunirá as regras comuns, como estados de missão e prevenção de pagamento duplicado.

6. Fases, tarefas e comprovação
Uma fase não é uma tarefa gigante
Cada arquivo em planning/phases/ deverá conter objetivo, pré-requisitos, entregas, limites, dependências, testes obrigatórios e critérios para encerrar a fase.

As tarefas menores surgirão em planning/tasks/, com nomes como:

docs/planning/tasks/TASK-0001-audit-windows-environment.md
docs/planning/tasks/TASK-0002-audit-base-mod.md
Esses nomes são exemplos de futuras fichas, não tarefas já executadas.

Cada ficha precisa delimitar objetivo, documentos de entrada, arquivos permitidos, contratos protegidos, validação, aceite e recuperação.

BACKLOG.md será o índice dessas fichas. Caso adotemos GitHub Issues ou outro gerenciador depois, escolheremos um registro oficial, evitando dois backlogs manuais concorrentes.

Rastreabilidade de ponta a ponta
TRACEABILITY.md deverá permitir seguir esta cadeia:

Requisito do produto
        ↓
Especificação de comportamento e contrato técnico
        ↓
Tarefa de implementação
        ↓
Teste correspondente
        ↓
Evidência de execução
        ↓
Aceite da fase
Isso permite responder concretamente: “Como sabemos que esse requisito foi atendido?”

Evidências só depois de executar
Em quality/evidence/, os registros aparecerão após testes reais:

docs/quality/evidence/RUN-0001/REPORT.md
O relatório deverá identificar versão do jogo, commit do mod, ambiente, cenário, procedimento, resultado esperado, resultado observado e referências de logs ou gravações.

Resumos e pequenos dados sanitizados podem ser versionados. Vídeos, dumps e grandes logs ficam em armazenamento de artefatos, com referências duráveis.

Compilação aprovada, teste unitário aprovado e experiência jogável aprovada são resultados diferentes. O roadmap já exige essa distinção. 

7. Como preparar os agentes sem escrever seus prompts ainda
A pasta docs/agents/ definirá o método de trabalho.

Documento	Função
READING_GUIDE.md	Indicar o conjunto mínimo de leitura e os documentos específicos de cada tipo de tarefa.
ROLES_AND_OWNERSHIP.md	Definir responsabilidades de implementação, revisão, QA e áreas críticas.
WORKFLOW_AND_HANDOFF.md	Padronizar início, execução, revisão e passagem de contexto.
DEFINITION_OF_READY.md	Definir quando uma tarefa está suficientemente clara para começar.
DEFINITION_OF_DONE.md	Definir o que precisa ser comprovado para encerrá-la.
TOOLS_AND_PERMISSIONS.md	Registrar ferramentas disponíveis, limites de acesso e operações que exigem autorização.
O agente não deverá ler tudo em toda tarefa
Na primeira sessão, ele consultará a apresentação do projeto, as regras, o estado e a visão geral do produto.

Depois, a leitura será direcionada:

Regras comuns
     ↓
Estado atual
     ↓
Ficha da tarefa
     ↓
Especificações pertinentes
     ↓
Implementação e validação
     ↓
Evidências e revisão
     ↓
Atualização do estado
Não presumiremos que todas as ferramentas carregam automaticamente todos os documentos. Cada entrada dedicada deverá encaminhar a leitura, e a passagem de contexto registrará o material consultado.

Áreas críticas terão responsáveis definidos
Controle do jogador, ciclo de vida de entidades e formato dos saves não deverão receber alterações concorrentes não coordenadas.

Trabalhos independentes de interface, documentação e testes de lógica poderão avançar em paralelo quando os contratos estiverem definidos.

Também distinguiria três estados independentes:

Dimensão	Exemplos
Documento	Rascunho, em revisão, aceito, substituído.
Tarefa	Planejada, pronta, em andamento, bloqueada, em revisão, concluída.
Validação	Não executada, passou, falhou, inconclusiva.
Um documento aceito pode especificar um recurso ainda não implementado. Uma auditoria concluída pode demonstrar incompatibilidade. Nenhuma dessas situações significa que o recurso está funcionando.

8. O que preparar agora e o que deixar para depois
Não precisamos concluir toda a documentação antes da primeira investigação técnica.

Momento	Documentos necessários
Antes do primeiro prompt de auditoria	Visão, PRD inicial, roadmap consolidado, índice, estado real, regras documentais, regras dos agentes, permissões, fase 00 e primeira ficha de tarefa.
Durante as fases 00–01	Auditorias, fontes, ambiente Windows, versões confirmadas, compilação real, arquitetura inicial, contratos de controle/persistência e estratégia de testes.
Antes de implementar cada recurso	Especificação pertinente, dependências, tarefa delimitada, cenários e critérios de aceite.
Conforme houver execução	Relatórios reais, problemas observados, estado atualizado, compatibilidade e changelog.
Antes da beta e publicação	Guias do jogador, empacotamento, migração, remoção segura, auditoria de distribuição e manutenção.
O primeiro agente de auditoria não precisa receber respostas que ainda desconhecemos. Ele precisa receber perguntas claras, limites de acesso e um procedimento para descobrir e comprovar as respostas.

9. Onde entrarão código, assets e arquivos locais
A árvore principal é documental. Quando começarmos a implementação, ela poderá conviver com estas áreas:

DriveTheCityII/
├── src/         # Código do mod.
├── tests/       # Testes automatizados.
├── assets/      # Recursos próprios ou autorizados.
├── tools/       # Scripts verificados de desenvolvimento.
├── artifacts/   # Builds, grandes logs e capturas.
└── .local/      # Configuração privada da máquina.
A organização interna do código será decidida após a auditoria. Não travaria agora uma estrutura de projeto Unity independente: estamos planejando um mod, e um eventual projeto auxiliar de assets terá justificativa e organização próprias.

artifacts/ e .local/ serão ignorados por padrão pelo Git. Saves principais e backups permanecerão fora do repositório e do alcance de tarefas comuns dos agentes.

Também não criaria antecipadamente uma coleção de prompts, agentes especializados ou automações de publicação. Primeiro definimos produto, contratos, permissões e comprovação; depois configuramos as ferramentas.

Minha recomendação é aprovar essa estrutura como mapa documental e começar apenas pela base pré-auditoria. Assim, os futuros prompts do Claude Code poderão ser objetivos: executar uma tarefa delimitada, consultar documentos específicos, respeitar contratos e apresentar evidências — sem tentar reconstruir todo o projeto a partir de uma mensagem enorme.


ESTRUTURA_DOCUMENTAL_Drive_the_City_II.md
Document
Certo! Eu devo preparar o ambiente para começar a escrever o prompt para o melty ou devo enviar um prompt para o melty agora?

Worked for 7s
Minha recomendação é preparar primeiro o ambiente Windows e, logo em seguida, enviar um prompt inicial de auditoria ao Claude Code através do Melty. Não precisamos desenvolver o jogo inteiro agora, mas também não precisamos terminar toda a documentação antes de começar.

O objetivo dessa primeira execução será verificar se conseguimos realmente combinar o Cities: Skylines II, o Beta Test Driving Mod e as referências de Motor Town.

1. Prepare o Windows
Preparação do ambiente

0/6


Instalar e executar Cities: Skylines II pelo menos uma vez.

Instalar e executar Motor Town: Behind The Wheel.

Instalar Melty pelo site oficial e confirmar que detecta os dois jogos.

Instalar e autenticar Claude Code no Windows.

Instalar Git e as ferramentas .NET necessárias ao projeto, após a auditoria de compatibilidade.

Criar uma cidade pequena de testes no CS2 e fazer backup do save.
Você pode obter o Melty no site oficial. A documentação informa que o fluxo de criação consiste em selecionar os jogos, copiar as instruções geradas e fornecê-las ao Claude Code ou Codex. 

Mashups and Mods

2. Selecione os dois jogos no Melty
Cities Skylines II | Pełna Wersja | Windows PC PL - Stan: Nowy 34.99PLN - Sklepy, Opinie, Ceny w Allegro
Cities: Skylines II

Jogo principal
Motor Town Behind The Wheel STEAM - PEŁNA WERSJA PC • Cena • Opinie - Allegro
Motor Town

Referência de carreira
Beta Test Driving Mod

Será analisado como código-fonte C# complementar, não como terceiro jogo selecionado no Melty.

Isso é importante porque o Melty não garante que conseguirá importar automaticamente a carreira ou a física do Motor Town, desenvolvido em outro motor.

3. O primeiro prompt que eu enviaria
Depois de selecionar os jogos no Melty, copie as instruções que ele gerar para o Claude Code e acrescente este prompt em inglês:

DRIVE THE CITY II — PHASE 0: TECHNICAL FEASIBILITY AND ENVIRONMENT AUDIT
You are the lead engineer and technical architect for Drive the City II, a planned Cities: Skylines II mod inspired by Streets of SimCity and the driving career experience of Motor Town: Behind The Wheel.

Project components
Cities: Skylines II — The primary game, world simulation, road network, traffic, and city-building environment.

Beta Test Driving Mod — The existing MIT-licensed C# mod to evaluate, fork, and potentially extend: https://github.com/northernst11-bot/BetaTestDrivingMod

Motor Town: Behind The Wheel — A gameplay and design reference for taxi jobs, deliveries, trucks, buses, vehicle ownership, progression, and transportation businesses.

Melty — The mashup development environment, subject to compatibility verification.

Target platform: Windows.

Ultimate product vision
The player builds a city in Cities: Skylines II, switches into a controllable character, walks through the city, enters a vehicle, drives through the existing road network, performs transportation jobs, earns money, purchases vehicles, and eventually operates a transportation business.

The city-building simulation must remain functional and existing save files must be protected.

Your task now
Perform Phase 0 only: environment discovery and technical feasibility assessment.

Do not attempt to build the complete game yet.

1. Inspect the development environment
Detect the installed games, versions, launchers, and directories.

Inspect the Melty environment and its generated instructions.

Determine whether this specific combination can be developed through Melty.

Identify the supported Cities: Skylines II modding toolchain.

Check the available compiler, .NET, Git, and other required tools.

Report missing dependencies rather than inventing installation paths.

2. Audit Beta Test Driving Mod
Inspect the repository, license, source code, dependencies, and supported game versions.

Identify the vehicle-control, camera, input, traffic, and collision systems.

Determine what is reusable and what must be implemented independently.

Assess the safest way to maintain an independent fork.

Build and run an isolated baseline if the environment permits.

3. Evaluate Motor Town
Treat Motor Town as a design and behavioral reference.

Identify the core systems needed for a complete driving career:

Taxi and passenger transport.

Delivery jobs and cargo.

Truck and bus driving.

Vehicle acquisition and rental.

Personal economy and experience.

Garages and maintenance.

Transportation businesses.

Do not assume access to its source code or the ability to import its proprietary assets or engine systems.

4. Investigate walking and driving integration
Evaluate the feasibility of:

A controllable on-foot character.

Third-person and first-person cameras.

Walking on sidewalks and terrain.

Entering and exiting vehicles.

Driving within existing Cities: Skylines II traffic.

Safe save/load and return to city-building mode.

Study relevant mods as technical references without copying code that lacks redistribution permission.

5. Establish project documentation
Use the agreed project structure and roadmap as the source of truth when available.

Create or update the essential Phase 0 documents, including:

PROJECT_STATUS.md

FEASIBILITY_MATRIX.md

BASE_MOD_AUDIT.md

MOTOR_TOWN_REFERENCE.md

MELTY_EVALUATION.md

WINDOWS_SETUP.md

TOOLCHAIN_MATRIX.md

RISKS_AND_OPEN_QUESTIONS.md

If the agreed documents are not locally available, request or locate them before replacing their contents. Do not invent completed test results.

6. Safety constraints
Do not modify original game installation files unnecessarily.

Do not overwrite existing Cities: Skylines II saves.

Do not install conflicting driving mods simultaneously.

Do not redistribute proprietary Motor Town or Cities: Skylines II assets.

Do not publish a Melty mashup at this stage.

Do not use unverified assumptions as successful test results.

Prefer reversible, isolated development steps.

Required final report
Deliver:

Environment readiness.

Verified toolchain and dependencies.

Technical feasibility findings.

Reusable Beta Test Driving Mod components.

Major integration risks.

Recommended project architecture.

Exact preparation required for Phase 1.

A GO / CONDITIONAL GO / NO-GO recommendation, supported by evidence.

Success means a verified and documented development foundation, not a prematurely generated game.

4. Minha ordem recomendada
Preparar Windows e instalar os dois jogos, Melty e Claude Code.
Selecionar CS2 + Motor Town no Melty e copiar as instruções geradas.
Criar a pasta DriveTheCityII e colocar nela o ROADMAP.md e a estrutura documental que já definimos.

Executar o prompt de auditoria acima no Claude Code, junto das instruções do Melty.
Analisar o relatório antes de autorizar a implementação da primeira versão.
Portanto, eu não enviaria ainda um prompt para criar o jogo completo. Enviaria esse primeiro prompt assim que o ambiente estivesse minimamente preparado.

Isso permitirá descobrir cedo se o Melty realmente traz alguma vantagem para essa combinação — e, caso não traga, continuar com o desenvolvimento do mod C# sem perder o planejamento que já construímos.

O melty é um programa isolado mais parecido com o Claude Desktop mas nao vejo a opção para escolher a pasta propriamente dita

Worked for 7s
Entendi! E você tem razão. O Melty funciona como um aplicativo independente no Windows, e não necessariamente como um editor de código tradicional com a opção Open Folder.

Verifiquei a documentação e ela confirma que o Melty trabalha com jogos selecionados e instruções para agentes de programação. Não encontrei uma opção documentada para escolher diretamente a pasta do projeto dentro do Melty. 

Mashups and Mods
+1

Isso muda um pouco nossa estratégia, mas não é um problema.

Como devemos trabalhar
Minha proposta é separar duas responsabilidades:

Melty

Selecionar Cities: Skylines II + Motor Town e fornecer as instruções de integração

Claude Code / Codex

Executar o desenvolvimento, usando a pasta do projeto no Windows

D:\Projetos\DriveTheCityII

Código C#, documentação, testes, histórico Git e builds do mod

O caminho D:\Projetos\DriveTheCityII é apenas uma sugestão. Podemos utilizar qualquer unidade com espaço suficiente.

Passo a passo que eu seguiria
No Melty, selecione Cities: Skylines II + Motor Town: Behind The Wheel.

Localize a opção de criar o mashup ou copiar as instruções para um agente.

Abra o Claude Code separadamente e escolha a pasta DriveTheCityII como diretório de trabalho.

Forneça ao Claude Code as instruções geradas pelo Melty, juntamente com nosso prompt de auditoria.

Oriente o Claude a manter todo o código e documentação do nosso projeto nessa pasta, sem alterar diretamente os arquivos originais dos jogos.

Se você utilizar Claude Code pelo terminal do Windows, poderá iniciá-lo diretamente na pasta:

New-Item -ItemType Directory -Force "D:\Projetos\DriveTheCityII"
Set-Location "D:\Projetos\DriveTheCityII"
claude

Se o próprio Melty disponibilizar um agente integrado na sua versão, precisamos verificar se ele permite definir um workspace ou acessar diretórios externos. Não vou presumir que exista uma configuração que a documentação não confirma.

O que preciso ver agora
Envie uma captura de tela da janela principal do Melty, mostrando onde você está escrevendo os prompts.

Quero verificar se essa interface que você está usando é o fluxo de criação de mashups, um agente integrado ou apenas a área de publicação. Assim poderei orientar você exatamente onde inserir o primeiro prompt e como fazer o agente trabalhar na pasta correta — sem obrigar você a mudar de aplicativo desnecessariamente.