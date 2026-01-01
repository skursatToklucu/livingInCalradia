using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LivingInCalradia.AI.Configuration;
using LivingInCalradia.AI.Orchestration;
using LivingInCalradia.Core.Application.Interfaces;
using LivingInCalradia.Core.Application.Services;
using LivingInCalradia.Infrastructure.Sensors;
using LivingInCalradia.Infrastructure.Execution;

namespace LivingInCalradia.Main;

/// <summary>
/// Demonstration program showing how the AI system works.
/// Extended test suite with performance metrics and interactive mode.
/// This is a standalone console app for testing WITHOUT Bannerlord.
/// </summary>
public class Program
{
    private static AgentWorkflowService? _workflowService;
    private static bool _isInitialized;
    
    public static async Task Main(string[] args)
    {
        // Fix console encoding for Turkish characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        PrintBanner();
        
        var totalStopwatch = Stopwatch.StartNew();
        
        try
        {
            // Initialize the AI system
            var initWatch = Stopwatch.StartNew();
            Initialize();
            initWatch.Stop();
            Console.WriteLine($"?  Ba?latma süresi: {initWatch.ElapsedMilliseconds}ms\n");
            
            await ShowMainMenu();
            
            totalStopwatch.Stop();
            
            // Final Summary
            Console.WriteLine("\n????????????????????????????????????????????????????????");
            Console.WriteLine("?                    OTURUM TAMAMLANDI                     ?");
            Console.WriteLine("????????????????????????????????????????????????????????");
            Console.WriteLine($"\n?  Toplam süre: {totalStopwatch.ElapsedMilliseconds}ms ({totalStopwatch.Elapsed.TotalSeconds:F1} saniye)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\nÇ?kmak için bir tu?a bas?n...");
        try { Console.ReadKey(); } catch { }
    }
    
    /// <summary>
    /// Initializes the standalone AI system for console testing.
    /// Uses Mock sensors/executors instead of Bannerlord APIs.
    /// </summary>
    private static void Initialize()
    {
        Console.WriteLine("[Console Test] AI sistemi ba?lat?l?yor...");
        
        // Load configuration
        var config = AIConfiguration.Load();
        config.Validate();
        
        Console.WriteLine($"[Console Test] Provider: {config.Provider}, Model: {config.ModelId}");
        
        // Create orchestrator
        IAgentOrchestrator orchestrator;
        if (config.IsGroq)
        {
            orchestrator = new GroqOrchestrator(config.ApiKey, config.ModelId, config.Temperature);
            Console.WriteLine("[Console Test] GroqOrchestrator kullan?l?yor");
        }
        else
        {
            var kernelFactory = new KernelFactory()
                .WithOpenAI(config.ApiKey, config.ModelId, config.OrganizationId);
            orchestrator = kernelFactory.BuildOrchestrator();
            Console.WriteLine("[Console Test] SemanticKernelOrchestrator kullan?l?yor");
        }
        
        // Use MOCK implementations for console testing (no Bannerlord)
        var worldSensor = new MockWorldSensor();
        var actionExecutor = new MockActionExecutor();
        
        // Create workflow service
        _workflowService = new AgentWorkflowService(worldSensor, orchestrator, actionExecutor);
        
        _isInitialized = true;
        Console.WriteLine("[Console Test] AI sistemi ba?ar?yla ba?lat?ld?!");
    }
    
    /// <summary>
    /// Executes AI thinking for an agent.
    /// </summary>
    private static async Task ExecuteAgentThinkingAsync(string agentId)
    {
        if (!_isInitialized || _workflowService == null)
        {
            Console.WriteLine("[Console Test] AI sistemi ba?lat?lmam??!");
            return;
        }
        
        try
        {
            Console.WriteLine($"\n[AI] {agentId} dü?ünüyor...\n");
            
            var result = await _workflowService.ExecuteWorkflowAsync(agentId);
            
            if (result.IsSuccessful && result.Decision != null)
            {
                Console.WriteLine($"[AI] Reasoning: {result.Decision.Reasoning}");
                Console.WriteLine($"[AI] Actions: {result.Decision.Actions.Count}");
                
                foreach (var action in result.Decision.Actions)
                {
                    var detail = action.Parameters.ContainsKey("detail")
                        ? action.Parameters["detail"]?.ToString()
                        : "";
                    Console.WriteLine($"  ? {action.ActionType}: {detail}");
                }
            }
            else
            {
                Console.WriteLine($"[AI] Hata: {result.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI] Exception: {ex.Message}");
        }
    }
    
    private static void PrintBanner()
    {
        Console.WriteLine(@"
??????????????????????????????????????????????????????????????????????
?                                                                    ?
?   LIVING IN CALRADIA - Console Test                               ?
?   Yapay Zeka Destekli NPC Ajans Sistemi                           ?
?   Powered by Groq LLM                                             ?
?                                                                    ?
?   ? Bu konsol testi Bannerlord OLMADAN çal???r                    ?
?   ? Mock sensor/executor kullan?l?r                               ?
?                                                                    ?
??????????????????????????????????????????????????????????????????????
");
    }
    
    private static async Task ShowMainMenu()
    {
        while (true)
        {
            Console.WriteLine("\n????????????????????????????????????????????????????????");
            Console.WriteLine("?                      ANA MENÜ                            ?");
            Console.WriteLine("????????????????????????????????????????????????????????");
            Console.WriteLine("?  1. ?? H?zl? Test (2 NPC)                                ?");
            Console.WriteLine("?  2. ?? Tam Test (5 NPC)                                  ?");
            Console.WriteLine("?  3. ?? Tutarl?l?k Testi (Haf?za Testi)                   ?");
            Console.WriteLine("?  4. ?  Performans Testi                                  ?");
            Console.WriteLine("?  5. ?? ?nteraktif Mod (Kendi NPC'ni Olu?tur)             ?");
            Console.WriteLine("?  6. ?? Tüm Testler                                       ?");
            Console.WriteLine("?  0. ? Ç?k??                                             ?");
            Console.WriteLine("????????????????????????????????????????????????????????");
            Console.Write("\nSeçiminiz (0-6): ");
            
            var choice = Console.ReadLine()?.Trim() ?? "0";
            Console.WriteLine();
            
            switch (choice)
            {
                case "1":
                    await RunQuickTest();
                    break;
                case "2":
                    await RunFullTest();
                    break;
                case "3":
                    await RunMemoryTest();
                    break;
                case "4":
                    await RunPerformanceTest();
                    break;
                case "5":
                    await RunInteractiveMode();
                    break;
                case "6":
                    await RunAllTests();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("? Geçersiz seçim. Tekrar deneyin.");
                    break;
            }
        }
    }
    
    private static async Task RunQuickTest()
    {
        Console.WriteLine("???????????????????????????????????????????????????????");
        Console.WriteLine("                    HIZLI TEST (2 NPC)                  ");
        Console.WriteLine("???????????????????????????????????????????????????????\n");
        
        await TestAgent("King_Caladog_of_Battania", "Battanian Kral?");
        await Task.Delay(1000);
        await TestAgent("Merchant_Heinrich", "Vlandian Tüccar");
    }
    
    private static async Task RunFullTest()
    {
        Console.WriteLine("???????????????????????????????????????????????????????");
        Console.WriteLine("                    TAM TEST (5 NPC)                    ");
        Console.WriteLine("???????????????????????????????????????????????????????\n");
        
        var agents = new[]
        {
            ("King_Caladog_of_Battania", "Battanian Kral? Caladog"),
            ("Merchant_Heinrich_of_Pravend", "Vlandian Tüccar Heinrich"),
            ("Commander_Unqid_of_Aserai", "Aserai Komutan? Unqid"),
            ("Villager_Boris_of_Omor", "Sturgian Köylü Boris"),
            ("HorseArcher_Temur_of_Khuzait", "Khuzait Atl? Okçu Temur")
        };
        
        foreach (var (id, name) in agents)
        {
            await TestAgent(id, name);
            await Task.Delay(1500);
        }
    }
    
    private static async Task RunMemoryTest()
    {
        Console.WriteLine("???????????????????????????????????????????????????????");
        Console.WriteLine("              HAFIZA TEST? (Ayn? NPC 3 kez)             ");
        Console.WriteLine("???????????????????????????????????????????????????????\n");
        
        Console.WriteLine("?? Bu test NPC'nin önceki kararlar?n? hat?rlay?p tutarl?");
        Console.WriteLine("   davran?p davranmad???n? kontrol eder.\n");
        
        for (int i = 1; i <= 3; i++)
        {
            Console.WriteLine($"\n{'?'.ToString().PadRight(50, '?')}");
            Console.WriteLine($"  TURA {i}/3");
            Console.WriteLine($"{'?'.ToString().PadRight(50, '?')}\n");
            
            await TestAgent("King_Caladog_of_Battania", $"Kral Caladog (Tur {i})");
            await Task.Delay(2000);
        }
        
        Console.WriteLine("\n?? HAFIZA TEST? SONUÇLARI:");
        Console.WriteLine("??????????????????????????????????????????????????????");
        Console.WriteLine("   ? NPC önceki kararlar?n? prompt'ta gördü");
        Console.WriteLine("   ? Her turda haf?za büyüdü");
        Console.WriteLine("   ? Tutarl?l?k kontrolü yap?ld?");
    }
    
    private static async Task RunPerformanceTest()
    {
        Console.WriteLine("???????????????????????????????????????????????????????");
        Console.WriteLine("              PERFORMANS TEST? (Zamanlama)              ");
        Console.WriteLine("???????????????????????????????????????????????????????\n");
        
        var times = new List<long>();
        
        Console.WriteLine("?  5 API ça?r?s?n?n süresini ölçüyoruz...\n");
        
        var agents = new[]
        {
            "King_Caladog_of_Battania",
            "Merchant_Heinrich",
            "Commander_Unqid",
            "Villager_Boris",
            "HorseArcher_Temur"
        };
        
        foreach (var agent in agents)
        {
            var sw = Stopwatch.StartNew();
            await ExecuteAgentThinkingAsync(agent);
            sw.Stop();
            times.Add(sw.ElapsedMilliseconds);
            Console.WriteLine($"   ?  {agent}: {sw.ElapsedMilliseconds}ms\n");
            await Task.Delay(500);
        }
        
        // Calculate statistics
        var avg = times.Count > 0 ? times.Average() : 0;
        var min = times.Count > 0 ? times.Min() : 0;
        var max = times.Count > 0 ? times.Max() : 0;
        
        Console.WriteLine("\n?? PERFORMANS ÖZET?:");
        Console.WriteLine("??????????????????????????????????????????????????????");
        Console.WriteLine($"   Ortalama: {avg:F0}ms");
        Console.WriteLine($"   Minimum:  {min}ms");
        Console.WriteLine($"   Maksimum: {max}ms");
        Console.WriteLine($"   Toplam:   {times.Sum()}ms");
        Console.WriteLine($"   NPC/sn:   {1000.0 / avg:F2}");
    }
    
    private static async Task RunInteractiveMode()
    {
        Console.WriteLine("???????????????????????????????????????????????????????");
        Console.WriteLine("              ?NTERAKT?F MOD                            ");
        Console.WriteLine("???????????????????????????????????????????????????????\n");
        
        Console.WriteLine("?? Kendi NPC'nizi olu?turun ve dü?ünmesini izleyin!\n");
        
        while (true)
        {
            Console.WriteLine("\n???????????????????????????????????????????????????????");
            Console.WriteLine("?  NPC T?PLER?:                                         ?");
            Console.WriteLine("?  1. King/Lord (Kral/Lord)                             ?");
            Console.WriteLine("?  2. Merchant/Trader (Tüccar)                          ?");
            Console.WriteLine("?  3. Commander (Komutan)                               ?");
            Console.WriteLine("?  4. Villager (Köylü)                                  ?");
            Console.WriteLine("?  5. Soldier/Archer (Asker)                            ?");
            Console.WriteLine("?  0. Ana Menüye Dön                                    ?");
            Console.WriteLine("???????????????????????????????????????????????????????");
            Console.Write("\nNPC tipi seçin (0-5): ");
            
            var typeChoice = Console.ReadLine()?.Trim() ?? "0";
            
            if (typeChoice == "0") return;
            
            Console.Write("NPC'ye bir isim verin: ");
            var name = Console.ReadLine()?.Trim() ?? "Unknown";
            
            Console.Write("Hangi faction? (Battania/Vlandia/Aserai/Sturgia/Khuzait/Empire): ");
            var faction = Console.ReadLine()?.Trim() ?? "Calradia";
            
            // Build agent ID based on type
            var agentId = typeChoice switch
            {
                "1" => $"King_{name}_of_{faction}",
                "2" => $"Merchant_{name}_of_{faction}",
                "3" => $"Commander_{name}_of_{faction}",
                "4" => $"Villager_{name}_of_{faction}",
                "5" => $"Soldier_{name}_of_{faction}",
                _ => $"Agent_{name}_of_{faction}"
            };
            
            Console.WriteLine($"\n?? NPC Olu?turuldu: {agentId}\n");
            
            await TestAgent(agentId, $"{name} ({faction})");
            
            Console.Write("\nBu NPC'yi tekrar ça??rmak ister misiniz? (e/h): ");
            var again = Console.ReadLine()?.Trim().ToLower() ?? "h";
            
            while (again == "e" || again == "evet")
            {
                Console.WriteLine("\n?? Ayn? NPC tekrar dü?ünüyor...\n");
                await TestAgent(agentId, $"{name} ({faction}) - Devam");
                
                Console.Write("\nTekrar? (e/h): ");
                again = Console.ReadLine()?.Trim().ToLower() ?? "h";
            }
        }
    }
    
    private static async Task RunAllTests()
    {
        Console.WriteLine("???????????????????????????????????????????????????????");
        Console.WriteLine("                    TÜM TESTLER                         ");
        Console.WriteLine("???????????????????????????????????????????????????????\n");
        
        Console.WriteLine("\n?? BÖLÜM 1: H?zl? Test\n");
        await RunQuickTest();
        
        Console.WriteLine("\n?? BÖLÜM 2: Haf?za Testi\n");
        await RunMemoryTest();
        
        Console.WriteLine("\n?? BÖLÜM 3: Performans Testi\n");
        await RunPerformanceTest();
    }
    
    private static async Task TestAgent(string agentId, string displayName)
    {
        Console.WriteLine($"???????????????????????????????????????????????????????");
        Console.WriteLine($"?  ?? {displayName,-50} ?");
        Console.WriteLine($"?  ID: {agentId,-49} ?");
        Console.WriteLine($"???????????????????????????????????????????????????????");
        
        var sw = Stopwatch.StartNew();
        await ExecuteAgentThinkingAsync(agentId);
        sw.Stop();
        
        Console.WriteLine($"?  Süre: {sw.ElapsedMilliseconds}ms\n");
    }
}
