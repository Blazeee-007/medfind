using MedFind.Domain.Entities;
using MedFind.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace MedFind.Infrastructure.Data;

/// <summary>
/// Seeds realistic data: 30 pharmacies in Bengaluru, 60 medicines, inventory rows.
/// verifiedAt within last 30 min so demos look live.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(MedFindDbContext db, ILogger logger)
    {
        // Idempotent — skip if already seeded
        if (await db.Users.AnyAsync()) return;

        logger.LogInformation("Seeding database...");
        var now = DateTimeOffset.UtcNow;

        // ── Admin user ──────────────────────────────────────────────────────
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@medfind.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2024"),
            FullName = "MedFind Admin",
            Role = "PharmacyAdmin",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(adminUser);

        var patientUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "patient@medfind.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient@2024"),
            FullName = "Demo Patient",
            Role = "Patient",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Users.Add(patientUser);

        // ── Medicines (60 realistic entries) ───────────────────────────────
        var medicines = GetMedicines(now);
        db.Medicines.AddRange(medicines);

        // ── Pharmacies (30 across Bengaluru) ──────────────────────────────
        var pharmacyData = GetPharmacyData();
        var pharmacies = new List<Pharmacy>();
        var pharmOwners = new List<User>();

        foreach (var (name, address, city, phone, lat, lng) in pharmacyData)
        {
            var owner = new User
            {
                Id = Guid.NewGuid(),
                Email = $"admin.{SanitizeName(name)}@medfind.dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2024"),
                FullName = $"Admin — {name}",
                Role = "PharmacyAdmin",
                CreatedAt = now,
                UpdatedAt = now,
            };
            pharmOwners.Add(owner);

            pharmacies.Add(new Pharmacy
            {
                Id = Guid.NewGuid(),
                OwnerUserId = owner.Id,
                Name = name,
                AddressLine = address,
                City = city,
                Phone = phone,
                Location = new Point(lng, lat) { SRID = 4326 },
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        db.Users.AddRange(pharmOwners);
        db.Pharmacies.AddRange(pharmacies);
        await db.SaveChangesAsync();

        // ── Inventory (each pharmacy gets a random subset of medicines) ───
        var rng = new Random(42); // deterministic seed for reproducibility
        var inventoryItems = new List<InventoryItem>();

        foreach (var pharmacy in pharmacies)
        {
            // Pick 15–40 random medicines per pharmacy
            var count = rng.Next(15, 41);
            var selectedMeds = medicines.OrderBy(_ => rng.Next()).Take(count).ToList();

            foreach (var med in selectedMeds)
            {
                var hasStock = rng.NextDouble() > 0.2; // 80% chance of stock
                var isStale = rng.NextDouble() < 0.1;  // 10% are stale (for demo contrast)
                var qty = hasStock ? rng.Next(1, 50) : 0;

                // verifiedAt within last 30 min for active, or null/old for stale
                DateTimeOffset? verifiedAt = hasStock
                    ? isStale
                        ? now.AddHours(-2) // older than stale window
                        : now.AddMinutes(-rng.Next(1, 29)) // fresh
                    : null;

                inventoryItems.Add(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    PharmacyId = pharmacy.Id,
                    MedicineId = med.Id,
                    Quantity = qty,
                    VerifiedAt = verifiedAt,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        db.InventoryItems.AddRange(inventoryItems);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded: {Users} users, {Pharmacies} pharmacies, {Medicines} medicines, {Inventory} inventory rows",
            await db.Users.CountAsync(),
            await db.Pharmacies.CountAsync(),
            await db.Medicines.CountAsync(),
            await db.InventoryItems.CountAsync());
    }

    private static string SanitizeName(string name)
    {
        var cleaned = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return cleaned.Length <= 20 ? cleaned : cleaned[..20];
    }

    private static List<Medicine> GetMedicines(DateTimeOffset now) =>
    [
        new() { Id = Guid.NewGuid(), Name = "Paracetamol 500mg", GenericName = "Paracetamol", Brand = "Crocin", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Paracetamol 650mg", GenericName = "Paracetamol", Brand = "Dolo 650", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Ibuprofen 400mg", GenericName = "Ibuprofen", Brand = "Brufen", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Ibuprofen 200mg", GenericName = "Ibuprofen", Brand = "Advil", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Amoxicillin 500mg", GenericName = "Amoxicillin", Brand = "Mox", Form = "capsule", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Azithromycin 500mg", GenericName = "Azithromycin", Brand = "Azee", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Cetirizine 10mg", GenericName = "Cetirizine", Brand = "Zyrtec", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Loratadine 10mg", GenericName = "Loratadine", Brand = "Claritin", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Metformin 500mg", GenericName = "Metformin", Brand = "Glycomet", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Metformin 1000mg", GenericName = "Metformin", Brand = "Glycomet GP", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Atorvastatin 10mg", GenericName = "Atorvastatin", Brand = "Lipitor", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Atorvastatin 20mg", GenericName = "Atorvastatin", Brand = "Storvas", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Amlodipine 5mg", GenericName = "Amlodipine", Brand = "Amlokind", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Lisinopril 10mg", GenericName = "Lisinopril", Brand = "Lisoril", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Omeprazole 20mg", GenericName = "Omeprazole", Brand = "Omez", Form = "capsule", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Pantoprazole 40mg", GenericName = "Pantoprazole", Brand = "Pan 40", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Ranitidine 150mg", GenericName = "Ranitidine", Brand = "Zinetac", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Ciprofloxacin 500mg", GenericName = "Ciprofloxacin", Brand = "Cipro", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Doxycycline 100mg", GenericName = "Doxycycline", Brand = "Doxy 1", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Metronidazole 400mg", GenericName = "Metronidazole", Brand = "Flagyl", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Diclofenac 50mg", GenericName = "Diclofenac", Brand = "Voveran", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Naproxen 250mg", GenericName = "Naproxen", Brand = "Naprosyn", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Aspirin 75mg", GenericName = "Aspirin", Brand = "Ecosprin", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Clopidogrel 75mg", GenericName = "Clopidogrel", Brand = "Plavix", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Losartan 50mg", GenericName = "Losartan", Brand = "Repace", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Telmisartan 40mg", GenericName = "Telmisartan", Brand = "Telma", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Glimepiride 2mg", GenericName = "Glimepiride", Brand = "Amaryl", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Insulin Glargine 100U", GenericName = "Insulin Glargine", Brand = "Lantus", Form = "injection", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Levothyroxine 50mcg", GenericName = "Levothyroxine", Brand = "Thyronorm", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Prednisolone 10mg", GenericName = "Prednisolone", Brand = "Wysolone", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Montelukast 10mg", GenericName = "Montelukast", Brand = "Montek LC", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Salbutamol Inhaler", GenericName = "Salbutamol", Brand = "Asthalin", Form = "inhaler", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Budesonide Inhaler", GenericName = "Budesonide", Brand = "Budecort", Form = "inhaler", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Folic Acid 5mg", GenericName = "Folic Acid", Brand = "Folvite", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Vitamin D3 60000 IU", GenericName = "Cholecalciferol", Brand = "Uprise D3", Form = "softgel", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Vitamin B12 500mcg", GenericName = "Cyanocobalamin", Brand = "Methylcobal", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Iron + Folic Acid", GenericName = "Ferrous Sulphate", Brand = "Fersolate", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Calcium 500mg + D3", GenericName = "Calcium Carbonate", Brand = "Shelcal", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Ondansetron 4mg", GenericName = "Ondansetron", Brand = "Emset", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Domperidone 10mg", GenericName = "Domperidone", Brand = "Domstal", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Loperamide 2mg", GenericName = "Loperamide", Brand = "Imodium", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "ORS Sachets", GenericName = "Oral Rehydration Salts", Brand = "Electral", Form = "powder", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Lactulose 10g/15ml", GenericName = "Lactulose", Brand = "Duphalac", Form = "syrup", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Clonazepam 0.5mg", GenericName = "Clonazepam", Brand = "Rivotril", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Alprazolam 0.25mg", GenericName = "Alprazolam", Brand = "Alprax", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Sertraline 50mg", GenericName = "Sertraline", Brand = "Zoloft", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Escitalopram 10mg", GenericName = "Escitalopram", Brand = "Nexito", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Pregabalin 75mg", GenericName = "Pregabalin", Brand = "Lyrica", Form = "capsule", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Gabapentin 300mg", GenericName = "Gabapentin", Brand = "Neurontin", Form = "capsule", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Tamsulosin 0.4mg", GenericName = "Tamsulosin", Brand = "Urimax", Form = "capsule", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Sildenafil 50mg", GenericName = "Sildenafil", Brand = "Viagra", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Chlorpheniramine 4mg", GenericName = "Chlorpheniramine", Brand = "Piriton", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Betahistine 16mg", GenericName = "Betahistine", Brand = "Vertin", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Acyclovir 400mg", GenericName = "Acyclovir", Brand = "Zovirax", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Fluconazole 150mg", GenericName = "Fluconazole", Brand = "Forcan", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Albendazole 400mg", GenericName = "Albendazole", Brand = "Zentel", Form = "tablet", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Ranitidine Syrup 75mg/5ml", GenericName = "Ranitidine", Brand = "Zantac", Form = "syrup", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Paracetamol Syrup 120mg/5ml", GenericName = "Paracetamol", Brand = "Calpol", Form = "syrup", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Amoxicillin Syrup 125mg/5ml", GenericName = "Amoxicillin", Brand = "Mox Syrup", Form = "syrup", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Cetirizine Syrup 5mg/5ml", GenericName = "Cetirizine", Brand = "Zyncet", Form = "syrup", CreatedAt = now, UpdatedAt = now },
        new() { Id = Guid.NewGuid(), Name = "Metformin 500mg SR", GenericName = "Metformin", Brand = "Glycomet SR", Form = "tablet", CreatedAt = now, UpdatedAt = now },
    ];

    // 30 pharmacies spread across Bengaluru (Indiranagar, Koramangala, Whitefield, etc.)
    private static List<(string Name, string Address, string City, string Phone, double Lat, double Lng)> GetPharmacyData() =>
    [
        ("Apollo Pharmacy — Indiranagar", "100 Feet Road, Indiranagar", "Bengaluru", "080-25290000", 12.9784, 77.6408),
        ("Green Cross Pharmacy", "12 MG Road", "Bengaluru", "080-22080000", 12.9757, 77.6072),
        ("MedPlus — Koramangala", "80 Feet Road, 4th Block, Koramangala", "Bengaluru", "080-41277654", 12.9352, 77.6245),
        ("Wellness Forever Pharmacy", "15 Brigade Road", "Bengaluru", "080-22125678", 12.9719, 77.6098),
        ("Suraksha Pharmacy", "Sarjapur Road, Bellandur", "Bengaluru", "080-67432210", 12.9271, 77.6773),
        ("Netmeds Express — HSR", "HSR Layout, Sector 2", "Bengaluru", "080-67834512", 12.9121, 77.6446),
        ("1mg Pharmacy — Whitefield", "ITPL Main Road, Whitefield", "Bengaluru", "080-62345678", 12.9826, 77.7355),
        ("Pharma Square", "Cunningham Road", "Bengaluru", "080-23344556", 12.9888, 77.5992),
        ("Sri Venkateshwara Medical", "Old Airport Road, Domlur", "Bengaluru", "080-25278901", 12.9602, 77.6397),
        ("Manipal Pharmacy", "HAL 2nd Stage, Indiranagar", "Bengaluru", "080-25235678", 12.9733, 77.6487),
        ("LifeCare Medicals", "Residency Road, Richmond Town", "Bengaluru", "080-22217890", 12.9636, 77.5962),
        ("Apollo Pharmacy — JP Nagar", "JP Nagar 7th Phase", "Bengaluru", "080-26531234", 12.8938, 77.5872),
        ("Fortis Pharmacy", "Bannerghatta Road, JP Nagar", "Bengaluru", "080-66214444", 12.8961, 77.5956),
        ("MedPlus — Marathahalli", "Marathahalli Bridge", "Bengaluru", "080-25434343", 12.9558, 77.7013),
        ("Healthway Pharmacy", "Jayanagar 4th Block", "Bengaluru", "080-26634567", 12.9282, 77.5833),
        ("Medicity — BTM Layout", "BTM 2nd Stage", "Bengaluru", "080-26779012", 12.9166, 77.6101),
        ("Cure & Care Pharmacy", "Electronic City Phase 1", "Bengaluru", "080-28521234", 12.8456, 77.6603),
        ("Sri Datta Medical", "Rajajinagar 1st Block", "Bengaluru", "080-23453456", 12.9922, 77.5571),
        ("Apollo Pharmacy — Malleshwaram", "18th Cross, Malleshwaram", "Bengaluru", "080-23563434", 13.0034, 77.5705),
        ("Vasan Health Care Pharmacy", "Seshadripuram Main Road", "Bengaluru", "080-23198899", 13.0027, 77.5811),
        ("Medkart Pharmacy — Hebbal", "Hebbal Flyover Road", "Bengaluru", "080-23453322", 13.0355, 77.5912),
        ("1mg Pharmacy — Yeshwanthpur", "Yeshwanthpur Circle", "Bengaluru", "080-23453311", 13.0225, 77.5452),
        ("Star Pharmacy", "RT Nagar Main Road", "Bengaluru", "080-23671234", 13.0217, 77.5951),
        ("Wellness Pharmacy — Yelahanka", "Yelahanka New Town", "Bengaluru", "080-23451234", 13.1015, 77.5963),
        ("Apollo Pharmacy — Electronic City", "Electronic City Phase 2", "Bengaluru", "080-28521111", 12.8392, 77.6764),
        ("City Medicals — Vijayanagar", "Vijayanagar Main Road", "Bengaluru", "080-23303434", 12.9712, 77.5316),
        ("Priyal Medical Stores", "Basavanagudi, Gandhi Bazaar", "Bengaluru", "080-26613434", 12.9459, 77.5745),
        ("Medplus — Banashankari", "Banashankari 3rd Stage", "Bengaluru", "080-26631234", 12.9239, 77.5570),
        ("Orange Health Pharmacy", "Bommanahalli, Hosur Road", "Bengaluru", "080-67345678", 12.8975, 77.6376),
        ("SastaSundar — Bellandur", "Bellandur Gate, ORR", "Bengaluru", "080-67890123", 12.9270, 77.6771),
    ];
}
