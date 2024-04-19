using SDTP1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        var path = @"C:\Users\Duarte Oliveira\source\repos\SDTP1\SDTP1\Tabela_Nomes.csv";
        var servicos = LoadServicosFromCsv(path);

        // Example update
        UpdateServico(servicos, "Duarte Oliveira", newName: "lala", newServico: "C1");

        // Write updated data back to CSV
        WriteServicosToCsv(path, servicos);

        foreach (var servico in servicos)
        {
            Console.WriteLine($"Nome: {servico.Name} / Servico: {servico.Servico}");
        }
    }

    private static List<ServicoModel> LoadServicosFromCsv(string path)
    {
        var servicos = new List<ServicoModel>();
        using (var reader = new StreamReader(File.OpenRead(path)))
        {
            reader.ReadLine(); // Skip header
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var values = line.Split(';');
                servicos.Add(new ServicoModel { Name = values[0], Servico = values[1] });
            }
        }
        return servicos;
    }

    private static void UpdateServico(List<ServicoModel> servicos, string name, string newName = null, string newServico = null)
    {
        var servico = servicos.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (servico != null)
        {
            if (!string.IsNullOrEmpty(newName))
                servico.Name = newName;
            if (!string.IsNullOrEmpty(newServico))
                servico.Servico = newServico;
        }
    }

    private static void WriteServicosToCsv(string path, List<ServicoModel> servicos)
    {
        using (var writer = new StreamWriter(File.Create(path)))
        {
            writer.WriteLine("Nome;Servico");
            foreach (var servico in servicos)
            {
                writer.WriteLine($"{servico.Name};{servico.Servico}");
            }
        }
    }
}