using CsvHelper.Configuration.Attributes;
using SDTP1;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics.Metrics;
using System.Security.AccessControl;


class Servidor
{
    private static Mutex mut = new Mutex();
    private const int numIterations = 1;
    private const int numThreads = 3;




    static void Main(string[] args)
    {
        //string clienteID = "Cl_0007";
        for (int i = 0; i < numThreads; i++)
        {
            Thread newThread = new Thread(new ThreadStart(Server));
            newThread.Name = string.Format("Thread{0}", i + 1);
            newThread.Start();
            // Carregar os dados do arquivo CSV
            //Server();
        }
    }

    static string GetFilePath(string servicoID)
    {
        string baseDirectory = @"C:\Users\Duarte Oliveira\source\repos\SDTP1\SDTP1"; // Ensure this is the base directory only
        return Path.Combine(baseDirectory, $"{servicoID}");
    }
    static void ThreadProc()
        {
            for (int i = 0; i < numIterations; i++)
            {
                Server();
            }
        }



        static void Server()
        {
            TcpListener server = null;
        try
        {
            Console.WriteLine("{0} is requesting the mutex",
                         Thread.CurrentThread.Name);
            mut.WaitOne();

            Console.WriteLine("{0} has entered the protected area",
                              Thread.CurrentThread.Name);

            // Set the TcpListener on port 13000.
            Int32 port = 13000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");

            // TcpListener server = new TcpListener(port);
            server = new TcpListener(localAddr, port);

            // Start listening for client requests.
            server.Start();

            // Buffer for reading data
            Byte[] bytes = new Byte[256];
            byte[] dataSend = new byte[256];
            byte[] dataReceive = new byte[256];
            string data = null;
            //string data2 = null;

            string enviaMsg = "mensagem inicial";
            byte[] responseBytes = Encoding.ASCII.GetBytes(enviaMsg);




            while (true)
            {
                Console.Write("Waiting for a connection... \n");

                // Perform a blocking call to accept requests.
                // You could also use server.AcceptSocket() here.
                using TcpClient client = server.AcceptTcpClient();
                //Console.WriteLine("100 OK!");

                data = null;

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();




                int i;

                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    //Console.WriteLine("Received: {0}", data);

                    //Servico_A();



                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    List<ClienteServicoAssociation> associations = CarregarAssociacoesCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico.csv");

                    string clienteID = data;

                    string servicoAssociado = VerificarServicoAssociado(associations, data);
                   
                    if(servicoAssociado == null)
                    {
                        int sA = 0;
                        int sB = 0;
                        int sC = 0;
                        int sD = 0;

                        // Percorre a lista de associacões para contar o número de IDs associados a cada servico
                        foreach (var ID in associations)
                        {
                            if (ID.ServicoID == "Servico_A")
                            {
                                sA++;
                            }
                            else if (ID.ServicoID == "Servico_B")
                            {
                                sB++;
                            }
                            else if (ID.ServicoID == "Servico_C")
                            {
                                sC++;
                            }
                            else if (ID.ServicoID == "Servico_D")
                            {
                                sD++;
                            }
                        }

                        // Número de IDs associados a cada servico
                        // Por exemplo, sA contem o número de IDs associados ao "Servico_A", e assim por diante

                        // Chame o metodo para adicionar um novo ID
                        
                        string servicoMenosIDs = null;
                        int menorNumeroIDs = int.MaxValue;

                        if (sA < menorNumeroIDs)
                        {
                            menorNumeroIDs = sA;
                            servicoMenosIDs = "Servico_A";
                        }

                        if (sB < menorNumeroIDs)
                        {
                            menorNumeroIDs = sB;
                            servicoMenosIDs = "Servico_B";
                        }

                        if (sC < menorNumeroIDs)
                        {
                            menorNumeroIDs = sC;
                            servicoMenosIDs = "Servico_C";
                        }

                        if (sD < menorNumeroIDs)
                        {
                            menorNumeroIDs = sD;
                            servicoMenosIDs = "Servico_D";
                        }
                        int totalIDs = sA + sB + sC + sD + 1;
                        // Adiciona o novo ID ao servico com o menor número de IDs associados
                        if (servicoMenosIDs != null)
                        {
                            AdicionarNovoID("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servidor.cs", servicoMenosIDs, totalIDs);
                            enviaMsg = $"O ClienteID '{totalIDs}' esta associado ao Servico '{servicoMenosIDs}'.";
                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                            stream.Write(responseBytes, 0, responseBytes.Length);
                        }
                        
                    }

                    enviaMsg = "Qual e o seu ID?";
                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                    stream.Write(responseBytes, 0, responseBytes.Length);

                    int bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                    string responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);


                    servicoAssociado = VerificarServicoAssociado(associations, responseData); 


                    if (servicoAssociado != null)
                    {
                        enviaMsg = $"O ClienteID '{data}' esta associado ao Servico '{servicoAssociado}'.";
                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                        if (servicoAssociado == "Servico_A")
                        {


                            mut.WaitOne();
                            try
                            {
                                List<Tarefa> tarefas = CarregarTarefasCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_A.csv");
                                // Verificar o estado da tarefa associada ao ClienteID

                                string estadoTarefa = VerificarEstadoTarefa(tarefas, clienteID);
                                string ultimoEstado = null;
                                Tarefa proximaTarefaNaoAlocada = null;

                                //string data = null;
                                //System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                                //stream.Write(msg, 0, msg.Length);

                                foreach (var tarefa in tarefas)
                                {
                                    if (tarefa.ClienteID == clienteID)
                                    {
                                        Console.WriteLine($"Tarefa {tarefa.TarefaID}: {tarefa.Descricao}, Estado: {tarefa.Estado}");
                                        ultimoEstado = tarefa.Estado; // Atualizar o estado da ultima tarefa encontrada
                                    }

                                    if (tarefa.Estado == "Nao alocado" && proximaTarefaNaoAlocada == null)
                                    {
                                        proximaTarefaNaoAlocada = tarefa; // Atualizar a próxima tarefa não alocada encontrada
                                    }

                                }

                                // Verificar se foi encontrada alguma tarefa associada ao ClienteID
                                if (ultimoEstado != null)
                                {
                                    enviaMsg = $"A última tarefa associada ao ClienteID '{clienteID}' possui o estado: {ultimoEstado}";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);


                                    Console.WriteLine("");
                                    if (ultimoEstado == "Concluido")
                                    {
                                        enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                        bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                        responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                        i = 0;

                                        while (i == 0)
                                        {
                                            // Se sim, verificar se existe uma próxima tarefa não alocada
                                            if (responseData == "Sim")
                                            {
                                                if (proximaTarefaNaoAlocada != null)
                                                {



                                                    // Alocar o cliente à próxima tarefa não alocada
                                                    proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                    enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    proximaTarefaNaoAlocada.Estado = "Em curso";
                                                    proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                    AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_A.csv", tarefas);
                                                    i = 1;

                                                }
                                                else
                                                {
                                                    enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);
                                                    i = 1;
                                                }
                                                if (responseData == "Nao")
                                                {
                                                    i = 1;
                                                    break;
                                                }
                                                else
                                                {
                                                    enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);
                                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                }

                                            }
                                        }
                                    }
                                    if (ultimoEstado == "Em curso")
                                    {
                                        enviaMsg = "Terminou a tarefa? (Sim/Nao)";
                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                        bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                        responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                        i = 0;

                                        while (i == 0)
                                        {
                                            // Se sim, verificar se existe uma próxima tarefa não alocada
                                            if (responseData == "Sim")
                                            {



                                                enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                                i = 0;
                                                while (i == 0)
                                                    if (responseData == "Sim")
                                                    {


                                                        if (proximaTarefaNaoAlocada != null)
                                                        {



                                                            // Alocar o cliente à próxima tarefa não alocada
                                                            proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                            enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                            stream.Write(responseBytes, 0, responseBytes.Length);

                                                            proximaTarefaNaoAlocada.Estado = "Em curso";
                                                            proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                            AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_A.csv", tarefas);
                                                            i = 1;

                                                        }
                                                        else
                                                        {
                                                            enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                            stream.Write(responseBytes, 0, responseBytes.Length);
                                                            i = 1;
                                                        }
                                                        if (responseData == "Nao")
                                                        {
                                                            i = 1;
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                            stream.Write(responseBytes, 0, responseBytes.Length);
                                                            bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                            responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Não foi encontrada nenhuma tarefa associada ao ClienteID '{clienteID}'.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                                        enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                                        bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                        responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                                        i = 0;

                                                        while (i == 0)
                                                        {
                                                            // Se sim, verificar se existe uma próxima tarefa não alocada
                                                            if (responseData == "Sim")
                                                            {
                                                                if (proximaTarefaNaoAlocada != null)
                                                                {



                                                                    // Alocar o cliente à próxima tarefa não alocada
                                                                    proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                                    enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                                    proximaTarefaNaoAlocada.Estado = "Em curso";
                                                                    proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                                    AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_A.csv", tarefas);
                                                                    i = 1;

                                                                }
                                                                else
                                                                {
                                                                    enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                    stream.Write(responseBytes, 0, responseBytes.Length);
                                                                    i = 1;
                                                                }
                                                                if (responseData == "Nao")
                                                                {
                                                                    i = 1;
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                    stream.Write(responseBytes, 0, responseBytes.Length);
                                                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                                }

                                                            }
                                                        }

                                                    }
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                Console.WriteLine("{0} is leaving the protected area", Thread.CurrentThread.Name);

                                // Release the Mutex.
                                mut.ReleaseMutex();
                                Console.WriteLine("{0} has released the mutex", Thread.CurrentThread.Name);
                            }
                        }

                        
                    }
                    if (servicoAssociado == "Servico_B")
                    {


                        mut.WaitOne();
                        try
                        {
                            List<Tarefa> tarefas = CarregarTarefasCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_B.csv");
                            // Verificar o estado da tarefa associada ao ClienteID

                            string estadoTarefa = VerificarEstadoTarefa(tarefas, clienteID);
                            string ultimoEstado = null;
                            Tarefa proximaTarefaNaoAlocada = null;

                            //string data = null;
                            //System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                            //stream.Write(msg, 0, msg.Length);

                            foreach (var tarefa in tarefas)
                            {
                                if (tarefa.ClienteID == clienteID)
                                {
                                    Console.WriteLine($"Tarefa {tarefa.TarefaID}: {tarefa.Descricao}, Estado: {tarefa.Estado}");
                                    ultimoEstado = tarefa.Estado; // Atualizar o estado da ultima tarefa encontrada
                                }

                                if (tarefa.Estado == "Nao alocado" && proximaTarefaNaoAlocada == null)
                                {
                                    proximaTarefaNaoAlocada = tarefa; // Atualizar a próxima tarefa não alocada encontrada
                                }

                            }

                            // Verificar se foi encontrada alguma tarefa associada ao ClienteID
                            if (ultimoEstado != null)
                            {
                                enviaMsg = $"A última tarefa associada ao ClienteID '{clienteID}' possui o estado: {ultimoEstado}";
                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                stream.Write(responseBytes, 0, responseBytes.Length);


                                Console.WriteLine("");
                                if (ultimoEstado == "Concluido")
                                {
                                    enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                    i = 0;

                                    while (i == 0)
                                    {
                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                        if (responseData == "Sim")
                                        {
                                            if (proximaTarefaNaoAlocada != null)
                                            {



                                                // Alocar o cliente à próxima tarefa não alocada
                                                proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                proximaTarefaNaoAlocada.Estado = "Em curso";
                                                proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_B.csv", tarefas);
                                                i = 1;

                                            }
                                            else
                                            {
                                                enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                i = 1;
                                            }
                                            if (responseData == "Nao")
                                            {
                                                i = 1;
                                                break;
                                            }
                                            else
                                            {
                                                enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                            }

                                        }
                                    }
                                }
                                if (ultimoEstado == "Em curso")
                                {
                                    enviaMsg = "Terminou a tarefa? (Sim/Nao)";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                    i = 0;

                                    while (i == 0)
                                    {
                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                        if (responseData == "Sim")
                                        {



                                            enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                            stream.Write(responseBytes, 0, responseBytes.Length);

                                            bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                            responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                            i = 0;
                                            while (i == 0)
                                                if (responseData == "Sim")
                                                {


                                                    if (proximaTarefaNaoAlocada != null)
                                                    {



                                                        // Alocar o cliente à próxima tarefa não alocada
                                                        proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                        enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                                        proximaTarefaNaoAlocada.Estado = "Em curso";
                                                        proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                        AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_B.csv", tarefas);
                                                        i = 1;

                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);
                                                        i = 1;
                                                    }
                                                    if (responseData == "Nao")
                                                    {
                                                        i = 1;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);
                                                        bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                        responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                    }
                                                }
                                                else
                                                {
                                                    enviaMsg = "Não foi encontrada nenhuma tarefa associada ao ClienteID '{clienteID}'.";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                                    i = 0;

                                                    while (i == 0)
                                                    {
                                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                                        if (responseData == "Sim")
                                                        {
                                                            if (proximaTarefaNaoAlocada != null)
                                                            {



                                                                // Alocar o cliente à próxima tarefa não alocada
                                                                proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                                enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                                proximaTarefaNaoAlocada.Estado = "Em curso";
                                                                proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                                AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_B.csv", tarefas);
                                                                i = 1;

                                                            }
                                                            else
                                                            {
                                                                enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                                i = 1;
                                                            }
                                                            if (responseData == "Nao")
                                                            {
                                                                i = 1;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                            }

                                                        }
                                                    }

                                                }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Console.WriteLine("{0} is leaving the protected area", Thread.CurrentThread.Name);

                            // Release the Mutex.
                            mut.ReleaseMutex();
                            Console.WriteLine("{0} has released the mutex", Thread.CurrentThread.Name);
                        }
                    }
                    if (servicoAssociado == "Servico_C")
                    {


                        mut.WaitOne();
                        try
                        {
                            List<Tarefa> tarefas = CarregarTarefasCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_C.csv");
                            // Verificar o estado da tarefa associada ao ClienteID

                            string estadoTarefa = VerificarEstadoTarefa(tarefas, clienteID);
                            string ultimoEstado = null;
                            Tarefa proximaTarefaNaoAlocada = null;

                            //string data = null;
                            //System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                            //stream.Write(msg, 0, msg.Length);

                            foreach (var tarefa in tarefas)
                            {
                                if (tarefa.ClienteID == clienteID)
                                {
                                    Console.WriteLine($"Tarefa {tarefa.TarefaID}: {tarefa.Descricao}, Estado: {tarefa.Estado}");
                                    ultimoEstado = tarefa.Estado; // Atualizar o estado da ultima tarefa encontrada
                                }

                                if (tarefa.Estado == "Nao alocado" && proximaTarefaNaoAlocada == null)
                                {
                                    proximaTarefaNaoAlocada = tarefa; // Atualizar a próxima tarefa não alocada encontrada
                                }

                            }

                            // Verificar se foi encontrada alguma tarefa associada ao ClienteID
                            if (ultimoEstado != null)
                            {
                                enviaMsg = $"A última tarefa associada ao ClienteID '{clienteID}' possui o estado: {ultimoEstado}";
                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                stream.Write(responseBytes, 0, responseBytes.Length);


                                Console.WriteLine("");
                                if (ultimoEstado == "Concluido")
                                {
                                    enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                    i = 0;

                                    while (i == 0)
                                    {
                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                        if (responseData == "Sim")
                                        {
                                            if (proximaTarefaNaoAlocada != null)
                                            {



                                                // Alocar o cliente à próxima tarefa não alocada
                                                proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                proximaTarefaNaoAlocada.Estado = "Em curso";
                                                proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_C.csv", tarefas);
                                                i = 1;

                                            }
                                            else
                                            {
                                                enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                i = 1;
                                            }
                                            if (responseData == "Nao")
                                            {
                                                i = 1;
                                                break;
                                            }
                                            else
                                            {
                                                enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                            }

                                        }
                                    }
                                }
                                if (ultimoEstado == "Em curso")
                                {
                                    enviaMsg = "Terminou a tarefa? (Sim/Nao)";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                    i = 0;

                                    while (i == 0)
                                    {
                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                        if (responseData == "Sim")
                                        {



                                            enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                            stream.Write(responseBytes, 0, responseBytes.Length);

                                            bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                            responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                            i = 0;
                                            while (i == 0)
                                                if (responseData == "Sim")
                                                {


                                                    if (proximaTarefaNaoAlocada != null)
                                                    {



                                                        // Alocar o cliente à próxima tarefa não alocada
                                                        proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                        enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                                        proximaTarefaNaoAlocada.Estado = "Em curso";
                                                        proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                        AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_C.csv", tarefas);
                                                        i = 1;

                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);
                                                        i = 1;
                                                    }
                                                    if (responseData == "Nao")
                                                    {
                                                        i = 1;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);
                                                        bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                        responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                    }
                                                }
                                                else
                                                {
                                                    enviaMsg = "Não foi encontrada nenhuma tarefa associada ao ClienteID '{clienteID}'.";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                                    i = 0;

                                                    while (i == 0)
                                                    {
                                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                                        if (responseData == "Sim")
                                                        {
                                                            if (proximaTarefaNaoAlocada != null)
                                                            {



                                                                // Alocar o cliente à próxima tarefa não alocada
                                                                proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                                enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                                proximaTarefaNaoAlocada.Estado = "Em curso";
                                                                proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                                AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_C.csv", tarefas);
                                                                i = 1;

                                                            }
                                                            else
                                                            {
                                                                enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                                i = 1;
                                                            }
                                                            if (responseData == "Nao")
                                                            {
                                                                i = 1;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                            }

                                                        }
                                                    }

                                                }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Console.WriteLine("{0} is leaving the protected area", Thread.CurrentThread.Name);

                            // Release the Mutex.
                            mut.ReleaseMutex();
                            Console.WriteLine("{0} has released the mutex", Thread.CurrentThread.Name);
                        }
                    }
                    if (servicoAssociado == "Servico_D")
                    {


                        mut.WaitOne();
                        try
                        {
                            List<Tarefa> tarefas = CarregarTarefasCSV("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_D.csv");
                            // Verificar o estado da tarefa associada ao ClienteID

                            string estadoTarefa = VerificarEstadoTarefa(tarefas, clienteID);
                            string ultimoEstado = null;
                            Tarefa proximaTarefaNaoAlocada = null;

                            //string data = null;
                            //System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                            //stream.Write(msg, 0, msg.Length);

                            foreach (var tarefa in tarefas)
                            {
                                if (tarefa.ClienteID == clienteID)
                                {
                                    Console.WriteLine($"Tarefa {tarefa.TarefaID}: {tarefa.Descricao}, Estado: {tarefa.Estado}");
                                    ultimoEstado = tarefa.Estado; // Atualizar o estado da ultima tarefa encontrada
                                }

                                if (tarefa.Estado == "Nao alocado" && proximaTarefaNaoAlocada == null)
                                {
                                    proximaTarefaNaoAlocada = tarefa; // Atualizar a próxima tarefa não alocada encontrada
                                }

                            }

                            // Verificar se foi encontrada alguma tarefa associada ao ClienteID
                            if (ultimoEstado != null)
                            {
                                enviaMsg = $"A última tarefa associada ao ClienteID '{clienteID}' possui o estado: {ultimoEstado}";
                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                stream.Write(responseBytes, 0, responseBytes.Length);


                                Console.WriteLine("");
                                if (ultimoEstado == "Concluido")
                                {
                                    enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                    i = 0;

                                    while (i == 0)
                                    {
                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                        if (responseData == "Sim")
                                        {
                                            if (proximaTarefaNaoAlocada != null)
                                            {



                                                // Alocar o cliente à próxima tarefa não alocada
                                                proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                proximaTarefaNaoAlocada.Estado = "Em curso";
                                                proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_D.csv", tarefas);
                                                i = 1;

                                            }
                                            else
                                            {
                                                enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                i = 1;
                                            }
                                            if (responseData == "Nao")
                                            {
                                                i = 1;
                                                break;
                                            }
                                            else
                                            {
                                                enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                            }

                                        }
                                    }
                                }
                                if (ultimoEstado == "Em curso")
                                {
                                    enviaMsg = "Terminou a tarefa? (Sim/Nao)";
                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                    i = 0;

                                    while (i == 0)
                                    {
                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                        if (responseData == "Sim")
                                        {



                                            enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                            responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                            stream.Write(responseBytes, 0, responseBytes.Length);

                                            bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                            responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                            i = 0;
                                            while (i == 0)
                                                if (responseData == "Sim")
                                                {


                                                    if (proximaTarefaNaoAlocada != null)
                                                    {



                                                        // Alocar o cliente à próxima tarefa não alocada
                                                        proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                        enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);

                                                        proximaTarefaNaoAlocada.Estado = "Em curso";
                                                        proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                        AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_D.csv", tarefas);
                                                        i = 1;

                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);
                                                        i = 1;
                                                    }
                                                    if (responseData == "Nao")
                                                    {
                                                        i = 1;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                        responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                        stream.Write(responseBytes, 0, responseBytes.Length);
                                                        bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                        responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                    }
                                                }
                                                else
                                                {
                                                    enviaMsg = "Não foi encontrada nenhuma tarefa associada ao ClienteID '{clienteID}'.";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    enviaMsg = "Pretende alocar uma nova tarefa? (Sim/Nao)";
                                                    responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                    stream.Write(responseBytes, 0, responseBytes.Length);

                                                    bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                    responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);

                                                    i = 0;

                                                    while (i == 0)
                                                    {
                                                        // Se sim, verificar se existe uma próxima tarefa não alocada
                                                        if (responseData == "Sim")
                                                        {
                                                            if (proximaTarefaNaoAlocada != null)
                                                            {



                                                                // Alocar o cliente à próxima tarefa não alocada
                                                                proximaTarefaNaoAlocada.ClienteID = clienteID;
                                                                enviaMsg = $"ClienteID '{clienteID}' alocado à próxima tarefa não alocada '{proximaTarefaNaoAlocada.TarefaID}' - '{proximaTarefaNaoAlocada.Descricao}'.";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);

                                                                proximaTarefaNaoAlocada.Estado = "Em curso";
                                                                proximaTarefaNaoAlocada.ClienteID = clienteID;

                                                                AtualizarTarefas("C:\\Users\\Duarte Oliveira\\source\\repos\\SDTP1\\SDTP1\\Servico_D.csv", tarefas);
                                                                i = 1;

                                                            }
                                                            else
                                                            {
                                                                enviaMsg = "Todas as tarefas se encontram alocadas.";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                                i = 1;
                                                            }
                                                            if (responseData == "Nao")
                                                            {
                                                                i = 1;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                enviaMsg = "Resposta Invalida!!!/nInsira novamente!";
                                                                responseBytes = Encoding.ASCII.GetBytes(enviaMsg);
                                                                stream.Write(responseBytes, 0, responseBytes.Length);
                                                                bytesReceived = stream.Read(dataReceive, 0, dataReceive.Length);
                                                                responseData = Encoding.ASCII.GetString(dataReceive, 0, bytesReceived);
                                                            }

                                                        }
                                                    }

                                                }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Console.WriteLine("{0} is leaving the protected area", Thread.CurrentThread.Name);

                            // Release the Mutex.
                            mut.ReleaseMutex();
                            Console.WriteLine("{0} has released the mutex", Thread.CurrentThread.Name);
                        }
                    }




                }
            }
        }







        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server.Stop();
        }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();

        }



   


    static List<ClienteServicoAssociation> CarregarAssociacoesCSV(string filePath)
    {
        List<ClienteServicoAssociation> associations = new List<ClienteServicoAssociation>();
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string clienteID = parts[0].Trim();
                    string servicoID = parts[1].Trim();
                    associations.Add(new ClienteServicoAssociation(clienteID, servicoID));
                }
            }
        }
        return associations;
    }


    static string VerificarServicoAssociado(List<ClienteServicoAssociation> associations, string clienteID)
    {
        ClienteServicoAssociation association = associations.FirstOrDefault(a => a.ClienteID == clienteID);
        return association != null ? association.ServicoID : null;
    }





    static List<Tarefa> CarregarTarefasCSV(string servicoID)
    {
        string filePath = GetFilePath(servicoID);
        List<Tarefa> tarefas = new List<Tarefa>();
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    tarefas.Add(new Tarefa(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), parts[3].Trim()));
                }
            }
        }
        return tarefas;
    }

    static string VerificarEstadoTarefa(List<Tarefa> tarefas, string clienteID)
    {
        Tarefa tarefa = tarefas.FirstOrDefault(t => t.ClienteID == clienteID);
        return tarefa != null ? tarefa.Estado : null;
    }





    static void AtualizarTarefas(string servicoID, List<Tarefa> tarefas)
    {
        string filePath = GetFilePath(servicoID);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("TarefaID,Descricao,Estado,ClienteID");
            foreach (var tarefa in tarefas)
            {
                writer.WriteLine($"{tarefa.TarefaID},{tarefa.Descricao},{tarefa.Estado},{tarefa.ClienteID}");
            }
        }
    }


    static void AdicionarNovoID(string filePath, string servicoMenosIDs, int novoID)
    {
        // Adquire o mutex para garantir exclusão mútua durante a escrita
        mut.WaitOne();
        try
        {
            // Abre o arquivo CSV no modo de acrescentar
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                // Escreve o novo ID no arquivo CSV com o servico associado
                writer.WriteLine($"{servicoMenosIDs},{novoID}");
            }
        }
        finally
        {
            // Libera o mutex após a conclusão da operacão
            mut.ReleaseMutex();
        }
    }


    class ClienteServicoAssociation
    {
        public string ClienteID { get; set; }
        public string ServicoID { get; set; }

        public ClienteServicoAssociation(string clienteID, string servicoID)
        {
            ClienteID = clienteID;
            ServicoID = servicoID;
        }
    }



    class Tarefa
    {
        public string TarefaID { get; set; }
        public string Descricao { get; set; }
        public string Estado { get; set; }
        public string ClienteID { get; set; }

        public Tarefa(string tarefaID, string descricao, string estado, string clienteID)
        {
            TarefaID = tarefaID;
            Descricao = descricao;
            Estado = estado;
            ClienteID = clienteID;
        }
    }
}