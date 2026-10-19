using System.Collections.Generic;

namespace DriveTheCityII.Core.Text
{
    /// <summary>
    /// Brazilian Portuguese for the career messages that Core reports as a stable "DTC.*" key plus an English text
    /// (command results, notifications, mission/leg/challenge events, recovery options, ledger lines, company headings).
    /// The mod shows them through CareerText.Of(key, english, args): the PT-BR template below is used when present,
    /// otherwise the English text from Core is shown unchanged.
    /// <para>
    /// Placeholders {0}, {1}... appear only for keys whose call sites pass CareerEvent.Args, and they match those Args
    /// exactly. Every other key is shown without Args, so its templates must not contain placeholders or braces.
    /// </para>
    /// <para>
    /// Deliberately NOT registered (they keep their English text), because the English text embeds values that are not
    /// passed as Args, or the Args cannot reproduce the English output:
    /// offer titles (DTC.Taxi.Offer.Title, DTC.Bus.Offer.Title, DTC.Delivery.Offer.*, DTC.Freight.Offer.*);
    /// DTC.Career.InternalError, DTC.Career.OfferError, DTC.Mission.SettlementPending (exception message);
    /// DTC.Mission.Invalidated.System (reason); DTC.Mission.VehicleIneligible (class list or reason);
    /// DTC.Mission.Locked and DTC.Garage.Locked (requirement varies by career or model);
    /// DTC.Mission.Settled and DTC.Company.Report (Args carry raw cents, the text shows formatted money);
    /// DTC.Challenge.Completed (reward amount not in Args); DTC.Company.Resigned (period count not in Args);
    /// DTC.Challenge.Unavailable (shown both with and without Args); DTC.Challenge.WrongClass, DTC.Company.WrongClass
    /// (vehicle class); DTC.Result.InsufficientFunds (amounts); DTC.Settle.Bus.Stops, DTC.Settle.Bus.Passengers,
    /// DTC.Settle.Delivery.Parcels, DTC.Settle.Freight.Loads, DTC.Settle.Taxi.Tip (counts or stars); ledger entries
    /// that embed names, amounts or the service type (ChallengeReward, CompanyFounding, CompanyIncome,
    /// DissolutionTransfer, Fuel, HiringFee, Maintenance, MissionSettlement, RecoveryService, RentalDeposit, RentalFee,
    /// Salary, Severance, TransferFromCompany, TransferToCompany, Upgrade, VehiclePurchase, VehicleSale).
    /// </para>
    /// </summary>
    public static class LocCareerMessages
    {
        private static bool s_Registered;
        private static readonly List<string> s_Keys = new List<string>();

        /// <summary>Keys registered by <see cref="Register"/>, in registration order.</summary>
        public static IReadOnlyList<string> Keys => s_Keys;

        public static void Register()
        {
            if (s_Registered)
            {
                return;
            }
            s_Registered = true;
            void A(string k, string en, string pt)
            {
                s_Keys.Add(k);
                Loc.Add(k, en, pt);
            }

            // ---------------------------------------------------------------- generic results
            A("DTC.Result.Ok", "Done.", "Pronto.");
            // Several call sites use this key with different wording ("This transfer/purchase/repair... was already ...").
            A("DTC.Result.AlreadyApplied", "This was already done.", "Isso já foi feito.");

            A("DTC.Career.NoWorld", "City data is not available.", "Os dados da cidade não estão disponíveis.");
            A("DTC.Career.ClockRegression", "The career clock went backwards; career timers are paused until it catches up.",
                "O relógio da carreira voltou no tempo; os cronômetros da carreira ficam pausados até ele se ajustar.");

            // ---------------------------------------------------------------- jobs (missions)
            A("DTC.Mission.Accepted", "Job accepted.", "Trabalho aceito.");
            A("DTC.Mission.Active", "Job started.", "Trabalho iniciado.");
            A("DTC.Mission.Completed", "Job completed.", "Trabalho concluído.");
            A("DTC.Mission.AlreadyActive", "Finish or cancel your current job first.", "Termine ou cancele seu trabalho atual primeiro.");
            A("DTC.Mission.AlreadySettled", "This job was already settled.", "Este trabalho já foi acertado.");
            A("DTC.Mission.NotTerminal", "This job has not ended yet.", "Este trabalho ainda não terminou.");
            // Two texts: "There is no job to cancel." / "There is no job to end."
            A("DTC.Mission.None", "There is no job in progress.", "Não há nenhum trabalho em andamento.");
            A("DTC.Mission.OfferExpired", "This offer is no longer available.", "Esta oferta não está mais disponível.");
            A("DTC.Mission.OfferWithdrawn", "Offer no longer available.", "Oferta não está mais disponível.");
            A("DTC.Mission.Ineligible", "You cannot take this job yet.", "Você ainda não pode pegar este trabalho.");
            // The English text may end with " (<reason>)", an internal caller tag ("player"); it is not repeated here.
            A("DTC.Mission.Cancelled.Player", "You cancelled the job.", "Você cancelou o trabalho.");
            A("DTC.Mission.Cancelled.StartWindow", "The job was cancelled because it was not started in time.",
                "O trabalho foi cancelado porque não foi iniciado a tempo.");
            A("DTC.Mission.Failed.Deadline", "The deadline passed before the job started.", "O prazo passou antes de o trabalho começar.");
            A("DTC.Mission.Failed.NothingDelivered", "Nothing could be delivered. The job failed.", "Nada pôde ser entregue. O trabalho falhou.");
            A("DTC.Mission.Failed.Abandoned", "You left the vehicle for too long. The job failed.",
                "Você ficou tempo demais fora do veículo. O trabalho falhou.");
            A("DTC.Mission.Invalidated.PlaceRemoved", "A destination was removed from the city. The job was cancelled with no fault.",
                "Um destino foi removido da cidade. O trabalho foi cancelado sem culpa sua.");
            A("DTC.Mission.Invalidated.InternalError", "The job hit an internal error and ended with no fault.",
                "O trabalho teve um erro interno e terminou sem culpa sua.");
            A("DTC.Mission.WaitingForYou", "Your passengers or cargo are waiting in the vehicle. Return soon.",
                "Seus passageiros ou sua carga estão esperando no veículo. Volte logo.");

            // Leg events carry Args = { place name }.
            A("DTC.Mission.Leg.Servicing", "Servicing {0}.", "Em atendimento: {0}.");
            A("DTC.Mission.Leg.Done", "Stop done: {0}.", "Parada concluída: {0}.");
            A("DTC.Mission.Leg.Interrupted", "Moving interrupted the stop. Stop again to continue.",
                "O movimento interrompeu a parada. Pare de novo para continuar.");

            // ---------------------------------------------------------------- taxi
            // Args = { passenger count } (not used by the text).
            A("DTC.Taxi.Boarded", "Your career passengers boarded.", "Seus passageiros embarcaram.");
            A("DTC.Taxi.Fail.Patience", "The passenger ran out of patience and left.", "O passageiro perdeu a paciência e foi embora.");
            A("DTC.Taxi.Fail.SevereContacts", "Your passengers demanded to get out after three severe collisions.",
                "Seus passageiros exigiram descer depois de três colisões graves.");
            A("DTC.Taxi.Fail.Immobile", "The taxi could not move, so the passengers left.", "O táxi não conseguia andar, então os passageiros desceram.");
            A("DTC.Taxi.Invalidated.Pickup", "The pickup point was removed from the city. The fare was cancelled with no fault.",
                "O ponto de embarque foi removido da cidade. A corrida foi cancelada sem culpa sua.");
            A("DTC.Taxi.Invalidated.Dropoff", "The destination was removed from the city. The fare was cancelled with no fault.",
                "O destino foi removido da cidade. A corrida foi cancelada sem culpa sua.");
            A("DTC.Taxi.Invalidated.DropoffPaid", "The destination was removed from the city. The passengers got out; you were paid for the distance covered.",
                "O destino foi removido da cidade. Os passageiros desceram; você recebeu pela distância percorrida.");
            // Args = { old destination, new destination }.
            A("DTC.Taxi.Substituted", "The destination '{0}' no longer exists. The passenger asked to go to '{1}' instead; the fare was recalculated.",
                "O destino \"{0}\" não existe mais. O passageiro pediu para ir a \"{1}\"; a corrida foi recalculada.");

            // ---------------------------------------------------------------- deliveries
            A("DTC.Delivery.Loaded", "Parcels loaded. Deliver them in any order.", "Volumes carregados. Entregue-os em qualquer ordem.");
            A("DTC.Delivery.ParcelDestroyed", "A parcel was destroyed by the impact.", "Um volume foi destruído pelo impacto.");
            A("DTC.Delivery.HardDeadline", "The hard deadline passed. Undelivered parcels are returned to the shipper.",
                "O prazo final passou. Os volumes não entregues voltam para o remetente.");
            A("DTC.Delivery.Invalidated.Pickup", "The pickup was removed from the city. The job was cancelled with no fault.",
                "O local de coleta foi removido da cidade. O trabalho foi cancelado sem culpa sua.");
            A("DTC.Delivery.Invalidated.AllSkipped", "No address of this job can be reached any more. The job was cancelled with no fault.",
                "Nenhum endereço deste trabalho pode mais ser alcançado. O trabalho foi cancelado sem culpa sua.");
            // Args = { address name }.
            A("DTC.Delivery.Skipped", "The address '{0}' can no longer be reached. Its parcels were returned with no penalty.",
                "O endereço \"{0}\" não pode mais ser alcançado. Os volumes dele foram devolvidos sem penalidade.");
            // Text = Progression.RequirementText(Unlock.VanRental) + " (needs a van)"; keep in sync with that requirement.
            A("DTC.Delivery.Ineligible.Van", "Requires Deliveries level 2 (needs a van)", "Requer Entregas nível 2 (precisa de uma van)");

            // ---------------------------------------------------------------- freight
            A("DTC.Freight.Loaded", "Cargo loaded. Payload affects braking and acceleration.", "Carga a bordo. O peso afeta a frenagem e a aceleração.");
            A("DTC.Freight.LoadLost", "The load was lost.", "A carga foi perdida.");
            A("DTC.Freight.HardDeadline", "The hard deadline passed. The cargo is returned to the shipper.",
                "O prazo final passou. A carga volta para o remetente.");
            A("DTC.Freight.Invalidated.Pickup", "The loading bay was removed from the city. The contract was cancelled with no fault.",
                "A doca de carga foi removida da cidade. O contrato foi cancelado sem culpa sua.");
            A("DTC.Freight.Invalidated.AllSkipped", "No drop-off of this contract can be reached any more. It was cancelled with no fault.",
                "Nenhum ponto de entrega deste contrato pode mais ser alcançado. Ele foi cancelado sem culpa sua.");
            // Args = { drop-off name }.
            A("DTC.Freight.Skipped", "The drop-off '{0}' can no longer be reached. The cargo was returned with no fee.",
                "O ponto de entrega \"{0}\" não pode mais ser alcançado. A carga foi devolvida sem taxa.");
            // Args = { old drop-off, new drop-off }.
            A("DTC.Freight.Substituted", "The drop-off '{0}' no longer exists. The cargo goes to '{1}' instead; pay was recalculated.",
                "O ponto de entrega \"{0}\" não existe mais. A carga vai para \"{1}\"; o pagamento foi recalculado.");

            // ---------------------------------------------------------------- bus
            A("DTC.Bus.Fail.SevereContacts", "Three severe collisions in one shift. The operator ended your shift.",
                "Três colisões graves em um turno. O contratante encerrou seu turno.");
            // Args = { stop name }.
            A("DTC.Bus.StopMissed", "Stop '{0}' was missed. Passengers complained.", "Você perdeu a parada \"{0}\". Os passageiros reclamaram.");
            A("DTC.Bus.StopSkipped", "Stop '{0}' can no longer be reached and was skipped with no fault.",
                "A parada \"{0}\" não pode mais ser alcançada e foi pulada sem culpa sua.");
            // Args = { line name }.
            A("DTC.Bus.Invalidated.LineRemoved", "Line {0} was removed. The shift ended with no fault; served stops were paid in full.",
                "A linha {0} foi removida. O turno terminou sem culpa sua; as paradas atendidas foram pagas integralmente.");
            A("DTC.Bus.Invalidated.LineChanged", "Line {0} no longer serves this shift. The shift ended with no fault; served stops were paid in full.",
                "A linha {0} não atende mais este turno. O turno terminou sem culpa sua; as paradas atendidas foram pagas integralmente.");
            // Args = { line name, stops removed, stops added }.
            A("DTC.Bus.LineModified", "Line {0} was modified: {1} stop(s) removed, {2} added. Passengers were moved to the nearest remaining stops.",
                "A linha {0} foi modificada: {1} parada(s) removida(s), {2} adicionada(s). Os passageiros foram levados às paradas restantes mais próximas.");

            // ---------------------------------------------------------------- settlement lines (static ones only)
            A("DTC.Settle.Taxi.Fare", "Fare", "Corrida");
            A("DTC.Settle.Taxi.PartialFare", "Fare for the distance covered", "Corrida pela distância percorrida");
            // Two texts: "Driving evaluation" (deliveries, freight) / "Trip evaluation" (bus).
            A("DTC.Settle.Evaluation", "Driving evaluation", "Avaliação da condução");
            A("DTC.Settle.Delivery.PartialParcels", "Delivered parcels at 50%", "Volumes entregues a 50%");
            A("DTC.Settle.Freight.PartialLoads", "Delivered loads at 50%", "Cargas entregues a 50%");
            A("DTC.Settle.ReturnFee", "Return fee", "Taxa de devolução");
            A("DTC.Settle.Bus.Punctuality", "Punctuality bonus", "Bônus de pontualidade");
            A("DTC.Settle.Bus.Served", "Served stops (paid in full)", "Paradas atendidas (pagas integralmente)");
            A("DTC.Settle.Bus.ServedHalf", "Served stops at 50%", "Paradas atendidas a 50%");

            // ---------------------------------------------------------------- garage and vehicles
            A("DTC.Garage.NotAtServicePoint", "Stop at a service point first, or call mobile service.",
                "Pare em um ponto de serviço primeiro ou chame o serviço móvel.");
            A("DTC.Garage.ServicePointGone", "This service point no longer exists. Use mobile service or another point.",
                "Este ponto de serviço não existe mais. Use o serviço móvel ou outro ponto.");
            // Four texts: "That vehicle is not available." / "Challenges need one of your career vehicles."
            A("DTC.Garage.VehicleNotFound", "That vehicle is not available. Use one of your career vehicles.",
                "Esse veículo não está disponível. Use um dos seus veículos de carreira.");
            A("DTC.Garage.Managed", "This vehicle is on a managed contract (simulated).", "Este veículo está em um contrato gerenciado (simulado).");
            A("DTC.Garage.NotRentable", "This model cannot be rented.", "Este modelo não pode ser alugado.");
            A("DTC.Garage.NotForSale", "This model is not for sale.", "Este modelo não está à venda.");
            // Text = Progression.RequirementText(Unlock.CompanyOnly).
            A("DTC.Garage.CompanyOnly", "Available to companies only", "Disponível apenas para empresas");
            // Three texts, all "repay your deferred charges first (before buying ...)".
            A("DTC.Garage.DeferredOpen", "Repay your deferred charges first.", "Quite suas cobranças adiadas primeiro.");
            A("DTC.Garage.NotARental", "This is not one of your rentals.", "Este não é um dos seus veículos alugados.");
            A("DTC.Garage.InMission", "Finish or cancel the job first.", "Termine ou cancele o trabalho primeiro.");
            // Three texts: "Leave the vehicle and finish the job first." / "The vehicle is being driven." /
            // "Leave the vehicle before selling it."
            A("DTC.Garage.InUse", "The vehicle is in use. Leave it and finish the job first.",
                "O veículo está em uso. Saia dele e termine o trabalho primeiro.");
            // Three texts: "You do not own this vehicle." / "Only vehicles you own can be contributed." /
            // "Only owned vehicles can be stored."
            A("DTC.Garage.NotOwned", "Only vehicles you own can do this.", "Só é possível com veículos próprios.");
            A("DTC.Garage.CompanyVehicle", "This vehicle belongs to your company.", "Este veículo pertence à sua empresa.");
            A("DTC.Garage.UpgradeOwnedOnly", "Only vehicles you own can be upgraded.", "Só veículos próprios podem receber melhorias.");
            A("DTC.Garage.UpgradeClass", "This upgrade does not fit this vehicle.", "Esta melhoria não serve para este veículo.");
            A("DTC.Garage.SlotUsed", "This slot already has an upgrade.", "Este espaço já tem uma melhoria.");
            A("DTC.Garage.MaxCapacity", "The garage is already at maximum capacity.", "A garagem já está na capacidade máxima.");
            A("DTC.Garage.Full", "The garage is full. Expand it or keep the vehicle parked.", "A garagem está cheia. Amplie-a ou deixe o veículo estacionado.");
            A("DTC.Garage.NotStored", "This vehicle is not in storage.", "Este veículo não está guardado.");
            A("DTC.Garage.Immobilized", "This vehicle cannot move.", "Este veículo não pode andar.");
            A("DTC.Garage.Immobilized.OutOfFuel", "The vehicle ran out of fuel. Call roadside fuel, a tow, or take the free basic rental.",
                "O veículo ficou sem combustível. Chame combustível na estrada, um reboque ou pegue o aluguel básico grátis.");
            A("DTC.Garage.Immobilized.Breakdown", "The vehicle broke down. Call a tow, a roadside quick fix, or take the free basic rental.",
                "O veículo quebrou. Chame um reboque, um reparo rápido na estrada ou pegue o aluguel básico grátis.");
            A("DTC.Garage.FuelLow", "Fuel below 20%.", "Combustível abaixo de 20%.");
            A("DTC.Garage.FuelCritical", "Fuel below 10%. Find a fuel point.", "Combustível abaixo de 10%. Procure um ponto de abastecimento.");
            A("DTC.Garage.LimpReserve", "Out of fuel: limp reserve active (2 km at 30 km/h).",
                "Sem combustível: reserva de emergência ativa (2 km a 30 km/h).");
            A("DTC.Garage.BasicReplaced", "The disabled basic rental was collected; a replacement was delivered.",
                "O aluguel básico parado foi recolhido; um substituto foi entregue.");
            A("DTC.Garage.RentalReturned", "Rental returned.", "Aluguel devolvido.");
            A("DTC.Garage.Sold", "Vehicle sold.", "Veículo vendido.");
            A("DTC.Garage.ServiceVehicle", "A service vehicle is waiting for you.", "Um veículo do contratante está esperando por você.");
            A("DTC.Garage.ServiceVehicleCollected", "The operator collected the disabled vehicle.", "O contratante recolheu o veículo parado.");
            A("DTC.Garage.ServiceVehicleReplaced", "The operator delivered a replacement vehicle. Cargo and passengers were transferred.",
                "O contratante entregou um veículo substituto. Carga e passageiros foram transferidos.");
            A("DTC.Garage.ServiceVehicleReturned", "The operator collects the service vehicle when you leave it.",
                "O contratante recolhe o veículo quando você sair dele.");

            // ---------------------------------------------------------------- "get back to work" recovery options
            A("DTC.Recovery.BasicRental", "Take the basic rental (free; 25% revenue share)", "Pegar o aluguel básico (grátis; 25% da receita)");
            A("DTC.Recovery.StarterJob", "Accept a starter taxi or courier job", "Aceitar um trabalho inicial de táxi ou entregas");
            A("DTC.Recovery.RoadsideFuel", "Roadside fuel", "Combustível na estrada");
            A("DTC.Recovery.QuickFix", "Roadside quick fix", "Reparo rápido na estrada");
            A("DTC.Recovery.Tow", "Tow to the nearest service point", "Rebocar até o ponto de serviço mais próximo");
            A("DTC.Recovery.Sell", "Sell the vehicle (optional)", "Vender o veículo (opcional)");
            A("DTC.Recovery.ReturnRental", "Return the rental remotely", "Devolver o aluguel a distância");
            A("DTC.Recovery.Unsupported", "That option is not available here.", "Essa opção não está disponível aqui.");

            // ---------------------------------------------------------------- home and garage place
            A("DTC.Home.NotFound", "This place cannot be used.", "Este local não pode ser usado.");
            A("DTC.Home.NotResidential", "Only residential buildings can be your home.", "Só prédios residenciais podem ser sua casa.");
            A("DTC.Home.Cooldown", "You can change your home again in a few minutes.", "Você poderá mudar de casa de novo em alguns minutos.");
            A("DTC.Home.GarageKind", "A transit stop cannot be a garage.", "Uma parada de transporte público não pode ser garagem.");
            A("DTC.Home.None", "Choose a home first.", "Escolha uma casa primeiro.");
            A("DTC.Home.MissionOpen", "Finish or cancel your job first.", "Termine ou cancele seu trabalho primeiro.");
            A("DTC.Home.ReturnCooldown", "Return home is cooling down.", "Aguarde um pouco para voltar para casa de novo.");
            // Args = { old home name }.
            A("DTC.Home.Demolished", "Your home at {0} no longer exists. A temporary base was set nearby; choose a new home.",
                "Sua casa em {0} não existe mais. Uma base temporária foi definida por perto; escolha uma nova casa.");
            A("DTC.Home.DemolishedNoBase", "Your home at {0} no longer exists. Choose a new home.", "Sua casa em {0} não existe mais. Escolha uma nova casa.");
            A("DTC.Home.GarageDemolished", "Your garage no longer exists. Stored vehicles were moved to the default storage.",
                "Sua garagem não existe mais. Os veículos guardados foram levados para o depósito padrão.");
            // Mod-side refusals; their English text already comes from profile.noResidentialNearby / garage.goToServicePoint.
            A("DTC.Home.NoResidential", "No residential building within 80 m.", "Nenhum prédio residencial a 80 m.");
            A("DTC.Home.NoServicePoint", "Go to a service point (blue markers: parking facilities and depots) to rent, buy, refuel or repair.",
                "Vá a um ponto de serviço (marcadores azuis: estacionamentos e garagens) para alugar, comprar, abastecer ou consertar.");

            // ---------------------------------------------------------------- challenges
            A("DTC.Challenge.PointCount", "Wrong number of points for this challenge type.", "Número de pontos incorreto para este tipo de desafio.");
            // Two texts: "A point no longer exists." / "City data could not confirm a point."
            A("DTC.Challenge.PointMissing", "A point no longer exists or could not be confirmed.", "Um ponto não existe mais ou não pôde ser confirmado.");
            A("DTC.Challenge.NotConnected", "The route is not connected by road.", "A rota não está conectada por ruas.");
            A("DTC.Challenge.NotFound", "No such challenge.", "Desafio não encontrado.");
            // Two texts: "A challenge is already running." / "Finish or abort the challenge first."
            A("DTC.Challenge.RunActive", "A challenge is already running. Finish or abort it first.",
                "Já há um desafio em andamento. Termine ou abandone o desafio primeiro.");
            A("DTC.Challenge.NoRun", "No challenge is running.", "Nenhum desafio em andamento.");
            // Args = { challenge name }.
            A("DTC.Challenge.RouteChanged", "The route of '{0}' changed. Earlier records are kept as 'route changed'.",
                "A rota de \"{0}\" mudou. Os recordes anteriores ficam marcados como \"rota alterada\".");
            // Challenge run events carry Args = { challenge name }.
            A("DTC.Challenge.Armed", "Challenge armed: {0}.", "Desafio preparado: {0}.");
            A("DTC.Challenge.Started", "Go!", "Já!");
            A("DTC.Challenge.Failed", "Challenge failed.", "Falha no desafio.");
            const string voidEn = "Challenge run void: no result, no penalty.";
            const string voidPt = "Tentativa anulada: sem resultado e sem penalidade.";
            A("DTC.Challenge.Aborted", voidEn, voidPt);
            A("DTC.Challenge.Void.RouteChanged", voidEn, voidPt);
            A("DTC.Challenge.Void.Building", voidEn, voidPt);
            A("DTC.Challenge.Void.Exit", voidEn, voidPt);
            A("DTC.Challenge.Void.Recovery", voidEn, voidPt);

            // ---------------------------------------------------------------- company
            A("DTC.Company.Heading", "Managed operations (simulated)", "Operações gerenciadas (simuladas)");
            A("DTC.Company.Explanation", "Your employees' work is calculated, not shown in the city.",
                "O trabalho dos seus funcionários é calculado, não mostrado na cidade.");
            A("DTC.Company.None", "You have no company.", "Você não tem uma empresa.");
            A("DTC.Company.Exists", "You already have a company.", "Você já tem uma empresa.");
            A("DTC.Company.InvalidName", "Company names need 3 to 32 displayable characters.",
                "O nome da empresa precisa ter de 3 a 32 caracteres visíveis.");
            // Text = Progression.RequirementText(Unlock.CompanyFounding); keep in sync with that requirement.
            A("DTC.Company.Locked", "Requires a total level of 10 and reputation 60", "Requer nível total 10 e reputação 60");
            A("DTC.Company.InArrears", "The company is in arrears: no new contracts, hires or purchases until its debt is paid.",
                "A empresa está inadimplente: sem novos contratos, contratações ou compras até a dívida ser paga.");
            // Two texts: "That headquarters location does not exist." (founding) / "Your company headquarters no longer
            // exists. Choose a new location; managed operations continue." (revalidation).
            A("DTC.Company.HqMissing", "The headquarters location no longer exists. Choose a new location.",
                "O local da sede não existe mais. Escolha um novo local.");
            A("DTC.Company.TransferAmount", "Enter a positive amount.", "Informe um valor positivo.");
            A("DTC.Company.CandidateGone", "This candidate is no longer available.", "Este candidato não está mais disponível.");
            A("DTC.Company.EmployeeNotFound", "No such employee.", "Funcionário não encontrado.");
            A("DTC.Company.ContractGone", "This contract is no longer offered.", "Este contrato não está mais disponível.");
            A("DTC.Company.ContractNotFound", "No such contract.", "Contrato não encontrado.");
            A("DTC.Company.AssignNotFound", "Employee, contract or vehicle not found.", "Funcionário, contrato ou veículo não encontrado.");
            A("DTC.Company.NotFleet", "Only company vehicles can run managed contracts.", "Só veículos da empresa podem atender contratos gerenciados.");
            A("DTC.Company.Reliability", "This employee's reliability is below the contract minimum.",
                "A confiabilidade deste funcionário está abaixo do mínimo do contrato.");
            A("DTC.Company.VehicleTaken", "This vehicle is assigned to another employee.", "Este veículo está alocado a outro funcionário.");

            // ---------------------------------------------------------------- ledger (static texts only)
            A("DTC.Ledger.InitialBalance", "Initial balance", "Saldo inicial");
            A("DTC.Ledger.Correction", "Diagnostics correction", "Correção de diagnóstico");
            A("DTC.Ledger.Deferred", "Deferred charge", "Cobrança adiada");
            A("DTC.Ledger.DeferredRepayment", "Deferred charge repayment", "Pagamento de cobrança adiada");
            A("DTC.Ledger.RentalShare", "Basic rental share (25%)", "Parte do aluguel básico (25%)");
            A("DTC.Ledger.DepositRefund", "Deposit refund", "Devolução da caução");
            A("DTC.Ledger.Deposit", "Deposit", "Caução");
            A("DTC.Ledger.ExtraHours", "Extra hours", "Horas extras");
            A("DTC.Ledger.FuelUsed", "Fuel used", "Combustível usado");
            A("DTC.Ledger.ConditionLost", "Condition lost", "Perda de estado");
            A("DTC.Ledger.Towing", "Remote return towing", "Reboque da devolução a distância");
            A("DTC.Ledger.RentalReturnCharge", "Rental return charges beyond the deposit", "Cobranças da devolução acima da caução");
            A("DTC.Ledger.GarageExpansion", "Garage expansion", "Ampliação da garagem");
            A("DTC.Ledger.TransferFromWallet", "Transfer from the owner's wallet", "Transferência da carteira do dono");
            A("DTC.Ledger.TransferToWallet", "Transfer to the owner's wallet", "Transferência para a carteira do dono");
            A("DTC.Ledger.CompanyDebt", "Debt repayment", "Pagamento de dívida");
            A("DTC.Ledger.ContractCancelPenalty", "Contract cancellation penalty", "Multa por cancelamento de contrato");
            A("DTC.Ledger.CompanyMaintenance", "Automatic maintenance", "Manutenção automática");
            A("DTC.Ledger.CompanyFuel", "Fuel (calculated)", "Combustível (calculado)");
            A("DTC.Ledger.CompanyPenalty", "Shortfall penalty", "Multa por meta não cumprida");

            // ---------------------------------------------------------------- one-time economy tutorial hooks (DTC.Tutorial.<name>)
            A("DTC.Tutorial.Immobilized", "Your vehicle cannot move. The 'Get back to work' panel always offers a free basic rental.",
                "Seu veículo não pode andar. O painel \"Voltar ao trabalho\" sempre oferece um aluguel básico grátis.");
            A("DTC.Tutorial.DeferredCharge", "You could not pay the full amount; the rest is a deferred charge repaid from future earnings.",
                "Você não pôde pagar o valor total; o restante virou uma cobrança adiada, paga com ganhos futuros.");
            A("DTC.Tutorial.FirstCost", "Costs such as fuel and repairs come out of your career wallet, never the city budget.",
                "Custos como combustível e reparos saem da sua carteira da carreira, nunca do orçamento da cidade.");
            A("DTC.Tutorial.BasicRental", "The basic rental needs no money and includes fuel; it takes 25% of each job's pay.",
                "O aluguel básico não exige dinheiro e inclui combustível; ele fica com 25% do pagamento de cada trabalho.");
            A("DTC.Tutorial.FirstPurchase", "You own this vehicle now: no revenue share, but you pay its fuel and repairs.",
                "Agora este veículo é seu: sem divisão de receita, mas você paga o combustível e os reparos.");
            A("DTC.Tutorial.FirstCancellation", "Cancelling has consequences (reputation, sometimes a fee) but never makes your career unrecoverable.",
                "Cancelar tem consequências (reputação, às vezes uma taxa), mas nunca deixa sua carreira sem saída.");
            A("DTC.Tutorial.FirstSettlement", "You were paid. The breakdown shows pay, tip, XP and reputation; your wallet is separate from the city budget.",
                "Você foi pago. O detalhamento mostra pagamento, gorjeta, XP e reputação; sua carteira é separada do orçamento da cidade.");
            A("DTC.Tutorial.Company", "Your company has its own account. Your employees' work is calculated, not shown in the city.",
                "Sua empresa tem conta própria. O trabalho dos seus funcionários é calculado, não mostrado na cidade.");
        }
    }
}
