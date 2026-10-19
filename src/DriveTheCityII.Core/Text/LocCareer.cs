namespace DriveTheCityII.Core.Text
{
    /// <summary>English and Brazilian Portuguese strings for careers, garage, company, challenges and profile UI.</summary>
    public static class LocCareer
    {
        private static bool s_Registered;

        public static void Register()
        {
            if (s_Registered)
            {
                return;
            }
            s_Registered = true;
            void A(string k, string en, string pt) => Loc.Add(k, en, pt);

            A("career.taxi", "Taxi", "Táxi");
            A("career.delivery", "Deliveries", "Entregas");
            A("career.freight", "Freight", "Frete");
            A("career.bus", "Bus", "Ônibus");
            A("career.disabled", "Career unavailable in this city", "Carreira indisponível nesta cidade");
            A("career.disabledShort", "Career unavailable", "Carreira indisponível");
            A("career.noRoadNearby", "No road lane nearby to place the vehicle. Walk closer to a street.", "Nenhuma faixa de rua por perto para colocar o veículo. Chegue mais perto de uma rua.");
            A("career.serviceVehicleReady", "{0} is waiting for you at the marked spot.", "{0} está esperando por você no local marcado.");

            A("class.car", "Car", "Carro");
            A("class.taxi", "Taxi", "Táxi");
            A("class.van", "Van", "Van");
            A("class.rigidtruck", "Rigid truck", "Caminhão");
            A("class.bus", "Bus", "Ônibus");

            A("leg.pickup", "Pick up", "Buscar");
            A("leg.dropoff", "Drop off", "Deixar");
            A("leg.load", "Load", "Carregar");
            A("leg.unload", "Unload", "Descarregar");
            A("leg.stop", "Stop", "Parada");
            A("leg.checkpoint", "Checkpoint", "Ponto de controle");
            A("leg.park", "Park", "Estacionar");

            A("pax.waiting", "waiting", "esperando");
            A("pax.aboard", "aboard", "a bordo");
            A("pax.alighted", "dropped off", "desembarcou");

            A("vkind.basicrental", "Basic rental", "Aluguel básico");
            A("vkind.premiumrental", "Rental", "Aluguel");
            A("vkind.owned", "Owned", "Próprio");
            A("vkind.service", "Employer's vehicle", "Veículo do contratante");

            A("medal.none", "no medal", "sem medalha");
            A("medal.bronze", "bronze", "bronze");
            A("medal.silver", "silver", "prata");
            A("medal.gold", "gold", "ouro");

            A("challenge.checkpointroute", "Checkpoint route", "Rota de pontos de controle");
            A("challenge.timetrial", "Time trial", "Contra o relógio");
            A("challenge.parking", "Parking challenge", "Desafio de estacionamento");

            A("common.on", "On", "Ligar");
            A("common.off", "Off", "Desligar");
            A("common.apply", "Apply", "Aplicar");
            A("common.choose", "Choose", "Escolher");
            A("common.free", "Free", "Grátis");

            A("tab.jobs", "Jobs", "Trabalhos");
            A("tab.garage", "Garage", "Garagem");
            A("tab.company", "Company", "Empresa");
            A("tab.challenges", "Challenges", "Desafios");
            A("tab.profile", "Profile", "Perfil");

            A("jobs.chooseCareer", "Career", "Carreira");
            A("jobs.active", "Active job: {0}", "Trabalho ativo: {0}");
            A("jobs.cancel", "Cancel", "Cancelar");
            A("jobs.oneAtATime", "One job at a time. Finish or cancel it to see new offers.", "Um trabalho por vez. Termine ou cancele para ver novas ofertas.");
            A("jobs.taxiDuty", "Taxi duty (street hails)", "Plantão de táxi (chamadas na rua)");
            A("jobs.taxiDutyHint", "While on duty in a taxi, passengers may wave you down.", "Em plantão num táxi, passageiros podem fazer sinal.");
            A("jobs.none", "No offers right now", "Nenhuma oferta agora");
            A("jobs.noneHint", "Only reachable jobs are offered. Try another career, drive elsewhere, or refresh.", "Só trabalhos alcançáveis são oferecidos. Tente outra carreira, vá para outro lugar ou atualize.");
            A("jobs.passengers", "{0} passenger(s)", "{0} passageiro(s)");
            A("jobs.parcels", "{0} parcel(s)", "{0} volume(s)");
            A("jobs.vehicleProvided", "vehicle provided", "veículo fornecido");
            A("jobs.accept", "Accept", "Aceitar");
            A("jobs.refresh", "Refresh", "Atualizar");
            A("jobs.refreshHint", "New offers near you", "Novas ofertas perto de você");

            A("obj.taxiDetail", "{0} passenger(s), {1}", "{0} passageiro(s), {1}");
            A("obj.deliveryDetail", "Delivered {0} of {1}", "Entregues {0} de {1}");
            A("obj.freightDetail", "Cargo {0} t", "Carga {0} t");
            A("obj.busDetail", "{0} · stops served {1} · on board {2}", "{0} · paradas atendidas {1} · a bordo {2}");
            A("obj.servicing", "in progress {0}%", "em andamento {0}%");
            A("obj.deadline", "{0} s left", "faltam {0} s");

            A("garage.at", "Service point: {0}", "Ponto de serviço: {0}");
            A("garage.goToServicePoint", "Go to a service point (blue markers: parking facilities and depots) to rent, buy, refuel or repair.",
                "Vá a um ponto de serviço (marcadores azuis: estacionamentos e garagens) para alugar, comprar, abastecer ou consertar.");
            A("garage.noFuelStations", "The city has no fuel stations; service points provide fuel and repairs.", "A cidade não tem postos; pontos de serviço fornecem combustível e reparos.");
            A("garage.status", "Fuel {0}% · condition {1}% · {2} km", "Combustível {0}% · estado {1}% · {2} km");
            A("garage.immobilized.outoffuel", "out of fuel", "sem combustível");
            A("garage.immobilized.breakdown", "broken down", "quebrado");
            A("garage.immobilized.none", string.Empty, string.Empty);
            A("garage.call", "Bring here", "Trazer aqui");
            A("garage.called", "{0} is parked on the street nearby.", "{0} está estacionado na rua perto de você.");
            A("garage.refuel", "Refuel", "Abastecer");
            A("garage.repair", "Repair", "Consertar");
            A("garage.return", "Return", "Devolver");
            A("garage.sell", "Sell", "Vender");
            A("garage.basicRental", "Basic rental compact", "Compacto de aluguel básico");
            A("garage.basicRentalHint", "Always available, even when broke: the fee is taken from what you earn.", "Sempre disponível, mesmo sem dinheiro: a taxa sai do que você ganhar.");
            A("garage.rent", "Rent", "Alugar");
            A("garage.rentDetail", "Fee {0} · deposit {1}", "Taxa {0} · caução {1}");
            A("garage.buy", "Buy", "Comprar");
            A("garage.buyDetail", "Price {0}", "Preço {0}");

            A("company.managedNote", "Managed operations: employees and fleet results are simulated, not vehicles driving in the city.",
                "Operação gerenciada: funcionários e frota são simulados, não veículos circulando na cidade.");
            A("company.foundTitle", "Found a transport company", "Fundar uma empresa de transporte");
            A("company.found", "Found", "Fundar");
            A("company.defaultName", "City Transport Co.", "Transportes da Cidade");
            A("company.cash", "Company cash {0}", "Caixa da empresa {0}");
            A("company.debt", "Debt {0}", "Dívida {0}");
            A("company.deposit", "Deposit {0}", "Depositar {0}");
            A("company.withdraw", "Withdraw {0}", "Retirar {0}");
            A("company.employee", "Skill {0} · reliability {1}% · salary {2}", "Habilidade {0} · confiabilidade {1}% · salário {2}");
            A("company.candidate", "Candidate: {0}", "Candidato: {0}");
            A("company.assigned", "assigned", "alocado");
            A("company.idle", "idle", "livre");
            A("company.fire", "Dismiss", "Dispensar");
            A("company.hire", "Hire", "Contratar");
            A("company.contract", "{0} contract · {1} jobs per period", "Contrato de {0} · {1} serviços por período");
            A("company.contractDetail", "{0} per job · {1} periods · done {2}", "{0} por serviço · {1} períodos · feitos {2}");
            A("company.offer", "Offer: {0} · {1} jobs per period", "Oferta: {0} · {1} serviços por período");
            A("company.buyFleet", "Fleet: {0}", "Frota: {0}");
            A("company.autoAssign", "Assign idle employees to free vehicles", "Alocar funcionários livres a veículos livres");
            A("company.autoMaintOn", "Auto maintenance on", "Manutenção automática ligada");
            A("company.autoMaintOff", "Auto maintenance off", "Manutenção automática desligada");

            A("challenges.hint", "Challenges use your current vehicle. Results are kept per route.", "Desafios usam seu veículo atual. Resultados ficam salvos por rota.");
            A("challenges.best", "best {0} s ({1})", "melhor {0} s ({1})");
            A("challenges.noRecord", "no record yet", "sem recorde ainda");
            A("challenges.start", "Start", "Iniciar");
            A("challenges.checkpoint", "Checkpoint {0} of {1}", "Ponto {0} de {1}");
            A("challenges.time", "{0} s", "{0} s");
            A("challenges.toStart", "Drive to the start", "Vá até a largada");
            A("challenges.newNearby", "New challenges near you", "Novos desafios perto de você");

            A("profile.wallet", "Wallet {0}", "Carteira {0}");
            A("profile.deferred", "Deferred charges {0}", "Cobranças adiadas {0}");
            A("profile.reputation", "Reputation {0}", "Reputação {0}");
            A("profile.xp", "{0} / {1} XP", "{0} / {1} XP");
            A("profile.home", "Home: {0}", "Casa: {0}");
            A("profile.noHome", "No home set", "Sem casa definida");
            A("profile.garage", "Garage: {0}", "Garagem: {0}");
            A("profile.setHome", "Set nearest house as home", "Definir casa mais próxima");
            A("profile.setGarage", "Set this service point as garage", "Definir este ponto como garagem");
            A("profile.goHome", "Go home", "Ir para casa");
            A("profile.noResidentialNearby", "No residential building within 80 m.", "Nenhum prédio residencial a 80 m.");
        }
    }
}
