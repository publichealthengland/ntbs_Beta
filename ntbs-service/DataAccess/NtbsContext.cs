﻿using System.Collections.Generic;
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ntbs_service.Helpers;
using ntbs_service.Models;
using ntbs_service.Models.Entities;
using ntbs_service.Models.Enums;
using ntbs_service.Models.ReferenceEntities;

namespace ntbs_service.DataAccess
{
    [AuditDbContext(IncludeEntityObjects = true)]
    public class NtbsContext : AuditDbContext
    {
        // Max Length for fields with enum -> string conversion configured.
        // Without this defaults to NVARCHAR(MAX) as field length.
        private const int EnumMaxLength = 30;

        public NtbsContext()
        {
        }

        public NtbsContext(DbContextOptions<NtbsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Country> Country { get; set; }
        public virtual DbSet<Ethnicity> Ethnicity { get; set; }
        public virtual DbSet<TBService> TbService { get; set; }
        public virtual DbSet<Hospital> Hospital { get; set; }
        public virtual DbSet<Notification> Notification { get; set; }
        public virtual DbSet<NotificationSite> NotificationSite { get; set; }
        public virtual DbSet<NotificationGroup> NotificationGroup { get; set; }
        public virtual DbSet<Site> Site { get; set; }
        public virtual DbSet<Region> Region { get; set; }
        public virtual DbSet<Sex> Sex { get; set; }
        public virtual DbSet<PHEC> PHEC { get; set; }
        public virtual DbSet<PostcodeLookup> PostcodeLookup { get; set; }
        public virtual DbSet<Occupation> Occupation { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<CaseManagerTbService> CaseManagerTbService { get; set; }
        public virtual DbSet<SampleType> SampleType { get; set; }
        public virtual DbSet<ManualTestType> ManualTestType { get; set; }
        public virtual DbSet<ManualTestResult> ManualTestResult { get; set; }
        public virtual DbSet<Alert> Alert { get; set; }
        public virtual DbSet<VenueType> VenueType { get; set; }
        public virtual DbSet<SocialContextVenue> SocialContextVenue { get; set; }
        public virtual DbSet<TreatmentEvent> TreatmentEvent { get; set; }
        public virtual DbSet<TreatmentOutcome> TreatmentOutcome { get; set; }
        public virtual DbSet<SocialContextAddress> SocialContextAddress { get; set; }
        public virtual DbSet<FrequentlyAskedQuestion> FrequentlyAskedQuestion { get; set; }
        public virtual DbSet<UserLoginEvent> UserLoginEvent { get; set; }
        public virtual DbSet<MBovisExposureToKnownCase> MBovisExposureToKnownCase { get; set; }
        public virtual DbSet<MBovisUnpasteurisedMilkConsumption> MBovisUnpasteurisedMilkConsumption { get; set; }
        public virtual DbSet<MBovisOccupationExposure> MBovisOccupationExposures { get; set; }
        public virtual DbSet<MBovisAnimalExposure> MBovisAnimalExposure { get; set; }

        public virtual void SetValues<TEntityClass>(TEntityClass entity, TEntityClass values)
        {
            this.Entry(entity).CurrentValues.SetValues(values);
        }

        public virtual void SetValues<TEntityClass>(TEntityClass entity, object values)
        {
            this.Entry(entity).CurrentValues.SetValues(values);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("name=ntbsContext");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<Country>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.HasIndex(e => new {e.IsLegacy, e.Name});
            });


            modelBuilder.Entity<Country>().HasData(
                Models.SeedData.Countries.GetCountriesArray()
            );

            modelBuilder.Entity<Ethnicity>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(50);

                entity.Property(e => e.Label).HasMaxLength(200);
            });

            modelBuilder.Entity<Ethnicity>().HasData(
                new Ethnicity { EthnicityId = 1, Code = "WW", Label = "White", Order = 1 },
                new Ethnicity { EthnicityId = 8, Code = "H", Label = "Indian", Order = 2 },
                new Ethnicity { EthnicityId = 9, Code = "J", Label = "Pakistani", Order = 3 },
                new Ethnicity { EthnicityId = 10, Code = "K", Label = "Bangladeshi", Order = 4 },
                new Ethnicity { EthnicityId = 11, Code = "L", Label = "Any other Asian background", Order = 5 },
                new Ethnicity { EthnicityId = 13, Code = "N", Label = "Black African", Order = 6 },
                new Ethnicity { EthnicityId = 12, Code = "M", Label = "Black Caribbean", Order = 7 },
                new Ethnicity { EthnicityId = 14, Code = "P", Label = "Any other Black Background", Order = 8 },
                new Ethnicity { EthnicityId = 16, Code = "R", Label = "Chinese", Order = 9 },
                new Ethnicity { EthnicityId = 6, Code = "F", Label = "Mixed - White and Asian", Order = 10 },
                new Ethnicity { EthnicityId = 5, Code = "E", Label = "Mixed - White and Black African", Order = 11 },
                new Ethnicity { EthnicityId = 4, Code = "D", Label = "Mixed - White and Black Caribbean", Order = 12 },
                new Ethnicity { EthnicityId = 7, Code = "G", Label = "Any other mixed background", Order = 13 },
                new Ethnicity { EthnicityId = 15, Code = "S", Label = "Any other ethnic group", Order = 14 },
                new Ethnicity { EthnicityId = Ethnicities.NotStatedId, Code = "Z", Label = "Not stated", Order = 15 }
            );

            modelBuilder.Entity<TBService>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(16);
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Name).HasMaxLength(200);
                /*
                    TB services have TB service AD group associated with them in a 1-1
                    mapping. PHEC AD groups are defined on the PHEC model itself, which has a many-to-one mapping to TB Service through TBServiceToPHEC.
                    AD groups have a length limit if 64 characters, see
                    https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2003/cc756101(v=ws.10)?redirectedfrom=MSDN#fqdn-length-limitations
                 */
                entity.Property(e => e.ServiceAdGroup).HasMaxLength(64);
                entity.HasIndex(e => e.ServiceAdGroup).IsUnique();
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => new {e.IsLegacy, e.Name});

                entity.HasOne(e => e.PHEC)
                    .WithMany()
                    .HasForeignKey(tb => tb.PHECCode);
                entity.HasData(GetTBServicesList());
            });

            modelBuilder.Entity<Hospital>().HasData(GetHospitalsList());

            /*
             * Converters do not nicely handle null values, outputting 'NULL' string into the database.
             * This isn't a major issue though, as the mapping in application is accurate.
             */
            var statusEnumConverter = new EnumToStringConverter<Status>();
            var dotStatusEnumConverter = new EnumToStringConverter<DotStatus>();
            var healthcareSettingEnumConverter = new EnumToStringConverter<HealthcareSetting>();
            var hivStatusEnumConverter = new EnumToStringConverter<HIVTestStatus>();
            var riskFactorEnumConverter = new EnumToStringConverter<RiskFactorType>();
            var notificationStatusEnumConverter = new EnumToStringConverter<NotificationStatus>();
            var denotificationReasonEnumConverter = new EnumToStringConverter<DenotificationReason>();
            var testResultEnumConverter = new EnumToStringConverter<Result>();
            var alertStatusEnumConverter = new EnumToStringConverter<AlertStatus>();
            var alertTypeEnumConverter = new EnumToStringConverter<AlertType>();
            var frequencyEnumConverter = new EnumToStringConverter<Frequency>();
            var treatmentEventTypeEnumConverter = new EnumToStringConverter<TreatmentEventType>();
            var treatmentOutcomeTypeEnumConverter = new EnumToStringConverter<TreatmentOutcomeType>();
            var treatmentOutcomeSubTypeEnumConverter = new EnumToStringConverter<TreatmentOutcomeSubType>();
            var transferReasonEnumConverter = new EnumToStringConverter<TransferReason>();
            var exposureSettingEnumConverter = new EnumToStringConverter<ExposureSetting>();
            var milkProductEnumConverter = new EnumToStringConverter<MilkProductType>();
            var consumptionFrequencyEnumConverter = new EnumToStringConverter<ConsumptionFrequency>();
            var occupationSettingEnumConverter = new EnumToStringConverter<OccupationSetting>();
            var animalTypeEnumConverter = new EnumToStringConverter<AnimalType>();
            var animalTbStatusEnumConverter = new EnumToStringConverter<AnimalTbStatus>();
            var treatmentRegimentEnumConverter = new EnumToStringConverter<TreatmentRegimen>();

            modelBuilder.Entity<PHEC>(entity =>
            {
                entity.HasKey(e => e.Code);
                entity.HasData(GetPHECList());
            });

            modelBuilder.Entity<LocalAuthority>(entity =>
            {
                entity.HasKey(e => e.Code);
                entity.HasData(GetLocalAuthoritiesList());
            });

            modelBuilder.Entity<LocalAuthorityToPHEC>(entity =>
            {
                entity.HasKey(e => new { e.PHECCode, e.LocalAuthorityCode });

                entity.HasOne(e => e.LocalAuthority)
                    .WithOne(x => x.LocalAuthorityToPHEC)
                    .HasForeignKey<LocalAuthorityToPHEC>(la => la.LocalAuthorityCode);

                entity.HasOne(e => e.PHEC)
                    .WithOne()
                    .HasForeignKey<LocalAuthorityToPHEC>(la => la.PHECCode);

                entity.HasData(GetPHECtoLA());
            });

            modelBuilder.Entity<PostcodeLookup>(entity =>
            {
                entity.HasKey(e => e.Postcode);
                entity.HasOne(e => e.LocalAuthority)
                    .WithMany(c => c.PostcodeLookups)
                    .HasForeignKey(ns => ns.LocalAuthorityCode);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.Group)
                    .WithMany(g => g.Notifications)
                    .HasForeignKey(e => e.GroupId);

                entity.OwnsOne(e => e.HospitalDetails, hospitalDetails =>
                {
                    hospitalDetails.Property(e => e.CaseManagerUsername)
                        .HasMaxLength(64);

                    hospitalDetails.HasOne(e => e.CaseManager);

                    hospitalDetails.ToTable("HospitalDetails");
                });

                entity.OwnsOne(e => e.PatientDetails, x =>
                {
                    x.HasOne(pd => pd.PostcodeLookup)
                    .WithOne()
                    .HasForeignKey<PatientDetails>(ns => ns.PostcodeToLookup);

                    x.ToTable("Patients");
                });

                entity.OwnsOne(e => e.ClinicalDetails, e =>
                {
                    e.Property(cd => cd.BCGVaccinationState)
                       .HasConversion(statusEnumConverter)
                       .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.DotStatus)
                        .HasConversion(dotStatusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.HomeVisitCarriedOut)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.HealthcareSetting)
                        .HasConversion(healthcareSettingEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.EnhancedCaseManagementStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.HIVTestState)
                        .HasConversion(hivStatusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.Notes)
                        .HasMaxLength(1000);
                    e.Property(c => c.TreatmentRegimen)
                        .HasConversion(treatmentRegimentEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    e.Property(c => c.TreatmentRegimenOtherDescription)
                        .HasMaxLength(100);
                    e.Property(c => c.EnhancedCaseManagementLevel)
                        .HasDefaultValue(0);
                    e.ToTable("ClinicalDetails");
                });

                entity.OwnsOne(e => e.PatientTBHistory).ToTable("PatientTBHistories");

                entity.OwnsOne(e => e.ContactTracing).ToTable("ContactTracing");

                entity.OwnsOne(e => e.SocialRiskFactors, x =>
                {
                    x.OwnsOne(c => c.RiskFactorDrugs, rf =>
                    {
                        rf.Property(e => e.Status)
                            .HasConversion(statusEnumConverter)
                            .HasMaxLength(EnumMaxLength);
                        rf.Property(e => e.Type)
                            .HasConversion(riskFactorEnumConverter)
                            .HasMaxLength(EnumMaxLength)
                            .HasDefaultValue(RiskFactorType.Drugs);
                        rf.ToTable("RiskFactorDrugs");
                    });

                    x.OwnsOne(c => c.RiskFactorHomelessness, rh =>
                    {
                        rh.Property(e => e.Status)
                            .HasConversion(statusEnumConverter)
                            .HasMaxLength(EnumMaxLength);
                        rh.Property(e => e.Type)
                            .HasConversion(riskFactorEnumConverter)
                            .HasMaxLength(EnumMaxLength)
                            .HasDefaultValue(RiskFactorType.Homelessness);
                        rh.ToTable("RiskFactorHomelessness");
                    });

                    x.OwnsOne(c => c.RiskFactorImprisonment, rh =>
                    {
                        rh.Property(e => e.Status)
                            .HasConversion(statusEnumConverter)
                            .HasMaxLength(EnumMaxLength);
                        rh.Property(e => e.Type)
                            .HasConversion(riskFactorEnumConverter)
                            .HasMaxLength(EnumMaxLength)
                            .HasDefaultValue(RiskFactorType.Imprisonment);
                        rh.ToTable("RiskFactorImprisonment");
                    });

                    x.OwnsOne(c => c.RiskFactorSmoking, rf =>
                    {
                        rf.Property(e => e.Status)
                            .HasConversion(statusEnumConverter)
                            .HasMaxLength(EnumMaxLength);
                        rf.Property(e => e.Type)
                            .HasConversion(riskFactorEnumConverter)
                            .HasMaxLength(EnumMaxLength)
                            .HasDefaultValue(RiskFactorType.Smoking);
                        rf.ToTable("RiskFactorSmoking");
                    });

                    x.Property(e => e.AlcoholMisuseStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    x.Property(e => e.MentalHealthStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    x.Property(e => e.AsylumSeekerStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    x.Property(e => e.ImmigrationDetaineeStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);

                    x.ToTable("SocialRiskFactors");
                });

                entity.Property(e => e.NotificationStatus)
                    .HasConversion(notificationStatusEnumConverter)
                    .HasMaxLength(EnumMaxLength);

                entity.OwnsOne(e => e.ImmunosuppressionDetails, i =>
                {
                    i.Property(e => e.Status)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    i.ToTable("ImmunosuppressionDetails");
                });

                entity.OwnsOne(e => e.TravelDetails).ToTable("TravelDetails");
                entity.OwnsOne(e => e.VisitorDetails).ToTable("VisitorDetails");
                entity.OwnsOne(e => e.ComorbidityDetails, cd =>
                {
                    cd.Property(e => e.DiabetesStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    cd.Property(e => e.HepatitisBStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    cd.Property(e => e.HepatitisCStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    cd.Property(e => e.LiverDiseaseStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    cd.Property(e => e.RenalDiseaseStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);

                    cd.ToTable("ComorbidityDetails");
                });

                entity.OwnsOne(e => e.DenotificationDetails, i =>
                {
                    i.Property(e => e.Reason)
                        .HasConversion(denotificationReasonEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    i.ToTable("DenotificationDetails");
                });

                entity.OwnsOne(e => e.MDRDetails, i =>
                {
                    i.Property(e => e.ExposureToKnownCaseStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    i.Property(e => e.CaseInUKStatus)
                        .HasConversion(statusEnumConverter)
                        .HasMaxLength(EnumMaxLength);
                    i.ToTable("MDRDetails");
                });
                
                entity.OwnsOne(e => e.DrugResistanceProfile)
                    .ToTable("DrugResistanceProfile");


                entity.HasIndex(e => e.NotificationStatus);

                entity.HasIndex(e => new { e.NotificationStatus, e.SubmissionDate });
                entity.HasIndex(e => e.LTBRID);
                entity.HasIndex(e => e.ETSID);
                entity.HasIndex(e => e.LTBRPatientId);
                entity.HasIndex(e => e.ClusterId);
            });

            modelBuilder.Entity<Region>(entity =>
            {
                entity.Property(e => e.Label).HasMaxLength(200);
            });


            modelBuilder.Entity<Sex>(entity =>
            {
                entity.Property(e => e.Label).HasMaxLength(200);
            });

            modelBuilder.Entity<Sex>().HasData(
                new Sex { SexId = 1, Label = "Male" },
                new Sex { SexId = 2, Label = "Female" },
                new Sex { SexId = Sexes.UnknownId, Label = "Unknown" }
            );

            modelBuilder.Entity<NotificationSite>(entity =>
            {
                entity.HasKey(e => new { e.NotificationId, e.SiteId });

                entity.HasOne(e => e.Site)
                    .WithMany(s => s.NotificationSites)
                    .HasForeignKey(ns => ns.SiteId);
            });

            modelBuilder.Entity<Site>().HasData(
                new Site { SiteId = (int)SiteId.PULMONARY, Description = "Pulmonary" },
                new Site { SiteId = (int)SiteId.BONE_SPINE, Description = "Spine" },
                new Site { SiteId = (int)SiteId.BONE_OTHER, Description = "Bone/joint: Other" },
                new Site { SiteId = (int)SiteId.CNS_MENINGITIS, Description = "Meningitis" },
                new Site { SiteId = (int)SiteId.CNS_OTHER, Description = "CNS: Other" },
                new Site { SiteId = (int)SiteId.OCULAR, Description = "Ocular" },
                new Site { SiteId = (int)SiteId.CRYPTIC, Description = "Cryptic disseminated" },
                new Site { SiteId = (int)SiteId.GASTROINTESTINAL, Description = "Gastrointestinal/peritoneal" },
                new Site { SiteId = (int)SiteId.GENITOURINARY, Description = "Genitourinary" },
                new Site { SiteId = (int)SiteId.LYMPH_INTRA, Description = "Intra-thoracic" },
                new Site { SiteId = (int)SiteId.LYMPH_EXTRA, Description = "Extra-thoracic" },
                new Site { SiteId = (int)SiteId.LARYNGEAL, Description = "Laryngeal" },
                new Site { SiteId = (int)SiteId.MILIARY, Description = "Miliary" },
                new Site { SiteId = (int)SiteId.PLEURAL, Description = "Pleural" },
                new Site { SiteId = (int)SiteId.PERICARDIAL, Description = "Pericardial" },
                new Site { SiteId = (int)SiteId.SKIN, Description = "Soft tissue/Skin" },
                new Site { SiteId = (int)SiteId.OTHER, Description = "Other extra-pulmonary" }
            );

            modelBuilder.Entity<Occupation>(entity =>
            {
                entity.Property(e => e.Role).HasMaxLength(40);
                entity.Property(e => e.Sector).HasMaxLength(40);
            });

            modelBuilder.Entity<Occupation>().HasData(
                Models.SeedData.Occupations.GetOccupations()
            );

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Username).HasMaxLength(64);
                entity.Property(e => e.FamilyName).HasMaxLength(64);
                entity.Property(e => e.GivenName).HasMaxLength(64);
                entity.Property(e => e.DisplayName).HasMaxLength(64);

                entity.HasKey(e => e.Username);
            });

            modelBuilder.Entity<CaseManagerTbService>(entity =>
            {
                entity.Property(e => e.CaseManagerUsername).HasMaxLength(64);
                entity.Property(e => e.TbServiceCode).HasMaxLength(16);

                entity.HasKey(e => new { e.CaseManagerUsername, e.TbServiceCode });

                entity.HasOne(e => e.CaseManager)
                    .WithMany(caseManager => caseManager.CaseManagerTbServices)
                    .HasForeignKey(e => e.CaseManagerUsername);

                entity.HasOne(e => e.TbService)
                    .WithMany(tbService => tbService.CaseManagerTbServices)
                    .HasForeignKey(e => e.TbServiceCode);
            });

            modelBuilder.Entity<ManualTestType>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(40);

                entity.HasData(Models.SeedData.ManualTestTypes.GetManualTestTypes());
            });

            modelBuilder.Entity<SampleType>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(40);
                entity.Property(e => e.Category).HasMaxLength(40);

                entity.HasData(Models.SeedData.SampleTypes.GetSampleTypes());
            });

            modelBuilder.Entity<ManualTestTypeSampleType>(entity =>
            {
                entity.HasKey(e => new { e.ManualTestTypeId, e.SampleTypeId });

                entity.HasOne(e => e.ManualTestType)
                    .WithMany(manualTestType => manualTestType.ManualTestTypeSampleTypes)
                    .HasForeignKey(e => e.ManualTestTypeId);

                entity.HasOne(e => e.SampleType)
                    .WithMany(sampleType => sampleType.ManualTestTypeSampleTypes)
                    .HasForeignKey(e => e.SampleTypeId);

                entity.HasData(Models.SeedData.ManualTestTypeSampleTypes.GetJoinDataManualTestTypeToSampleType());
            });

            modelBuilder.Entity<TestData>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasMany(e => e.ManualTestResults);
            });
            
            modelBuilder.Entity<ManualTestResult>(entity =>
            {
                entity.Property(e => e.Result)
                    .HasConversion(testResultEnumConverter)
                    .HasMaxLength(EnumMaxLength);

                entity.HasOne(e => e.ManualTestType)
                    .WithMany()
                    .HasForeignKey(e => e.ManualTestTypeId);

                entity.HasOne(e => e.SampleType)
                    .WithMany()
                    .HasForeignKey(e => e.SampleTypeId);
            });

            modelBuilder.Entity<MBovisDetails>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasMany(e => e.MBovisExposureToKnownCases);
            });

            modelBuilder.Entity<MBovisExposureToKnownCase>(entity =>
            {
                entity.Property(e => e.ExposureSetting)
                    .HasConversion(exposureSettingEnumConverter)
                    .HasMaxLength(EnumMaxLength);
            });

            modelBuilder.Entity<MBovisUnpasteurisedMilkConsumption>(entity =>
            {
                entity.Property(e => e.ConsumptionFrequency)
                    .HasConversion(consumptionFrequencyEnumConverter)
                    .HasMaxLength(EnumMaxLength);

                entity.Property(e => e.MilkProductType)
                    .HasConversion(milkProductEnumConverter)
                    .HasMaxLength(EnumMaxLength);
            });
            
            modelBuilder.Entity<MBovisOccupationExposure>(entity =>
            {
                entity.Property(e => e.OccupationSetting)
                    .HasConversion(occupationSettingEnumConverter)
                    .HasMaxLength(EnumMaxLength);
            });

            modelBuilder.Entity<MBovisAnimalExposure>(entity =>
            {
                entity.Property(e => e.AnimalType)
                    .HasConversion(animalTypeEnumConverter)
                    .HasMaxLength(EnumMaxLength);
                
                entity.Property(e => e.AnimalTbStatus)
                    .HasConversion(animalTbStatusEnumConverter)
                    .HasMaxLength(EnumMaxLength);
            });

            modelBuilder.Entity<Alert>(entity =>
            {
                entity.Property(e => e.AlertStatus)
                    .HasConversion(alertStatusEnumConverter)
                    .HasMaxLength(EnumMaxLength);
                entity.Property(e => e.CaseManagerUsername).HasMaxLength(64);
                entity.Property(e => e.TbServiceCode).IsRequired().HasMaxLength(16);
                entity.Property(e => e.ClosingUserId).HasMaxLength(64);
                entity.HasIndex(p => new { p.NotificationId, p.AlertType });
                entity.Property(e => e.AlertType)
                    .HasConversion(alertTypeEnumConverter);
                entity.HasDiscriminator<AlertType>("AlertType")
                    .HasValue<TestAlert>(AlertType.Test)
                    .HasValue<MdrAlert>(AlertType.EnhancedSurveillanceMDR)
                    .HasValue<MBovisAlert>(AlertType.EnhancedSurveillanceMBovis)
                    .HasValue<TransferAlert>(AlertType.TransferRequest)
                    .HasValue<TransferRejectedAlert>(AlertType.TransferRejected)
                    .HasValue<UnmatchedLabResultAlert>(AlertType.UnmatchedLabResult)
                    .HasValue<DataQualityDraftAlert>(AlertType.DataQualityDraft)
                    .HasValue<DataQualityBirthCountryAlert>(AlertType.DataQualityBirthCountry)
                    .HasValue<DataQualityClinicalDatesAlert>(AlertType.DataQualityClinicalDates)
                    .HasValue<DataQualityClusterAlert>(AlertType.DataQualityCluster)
                    .HasValue<DataQualityTreatmentOutcome12>(AlertType.DataQualityTreatmentOutcome12)
                    .HasValue<DataQualityTreatmentOutcome24>(AlertType.DataQualityTreatmentOutcome24)
                    .HasValue<DataQualityTreatmentOutcome36>(AlertType.DataQualityTreatmentOutcome36);

                entity.HasIndex(e => new { e.AlertStatus, e.AlertType, e.TbServiceCode });
            });

            modelBuilder.Entity<TestAlert>().HasBaseType<Alert>();
            modelBuilder.Entity<TransferAlert>(entity =>
            {
                entity.Property(e => e.TransferReason)
                    .HasConversion(transferReasonEnumConverter)
                    .HasMaxLength(EnumMaxLength);
            });
            modelBuilder.Entity<UnmatchedLabResultAlert>(entity =>
            {
                entity.Property(e => e.SpecimenId).HasMaxLength(50);
            });

            modelBuilder.Entity<VenueType>().HasData(
                Models.SeedData.Venues.GetTypes()
            );

            modelBuilder.Entity<SocialContextVenue>(entity =>
            {
                entity.Property(e => e.Frequency)
                    .HasConversion(frequencyEnumConverter)
                    .HasMaxLength(EnumMaxLength);
            });

            modelBuilder.Entity<TreatmentEvent>(entity =>
            {
                entity.Property(e => e.TreatmentEventType)
                    .HasConversion(treatmentEventTypeEnumConverter)
                    .HasMaxLength(EnumMaxLength);
                entity.HasOne(e => e.CaseManager)
                    .WithMany()
                    .HasForeignKey(e => e.CaseManagerUsername);
                entity.HasOne(e => e.TbService)
                    .WithMany()
                    .HasForeignKey(e => e.TbServiceCode);
            });

            modelBuilder.Entity<TreatmentOutcome>(entity =>
            {
                entity.Property(e => e.TreatmentOutcomeType)
                    .HasConversion(treatmentOutcomeTypeEnumConverter)
                    .HasMaxLength(EnumMaxLength);

                entity.Property(e => e.TreatmentOutcomeSubType)
                    .HasConversion(treatmentOutcomeSubTypeEnumConverter)
                    .HasMaxLength(EnumMaxLength);

                entity.HasData(Models.SeedData.TreatmentOutcomes.GetTreatmentOutcomes());
            });

            modelBuilder.HasSequence<int>("OrderIndex", schema: "shared");
            modelBuilder.Entity<FrequentlyAskedQuestion>(entity =>
            {
                entity.Property(e => e.OrderIndex)
                    .HasDefaultValueSql("NEXT VALUE FOR shared.OrderIndex");
            });
            
            modelBuilder.Entity<UserLoginEvent>(entity =>
            {
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(64);
                entity.Property(e => e.LoginDate)
                    .IsRequired();
                entity.Property(e => e.SystemName)
                    .IsRequired()
                    .HasMaxLength(64);
            });
        }

        private static List<object> GetTBServicesList()
        {
            return SeedingHelper.GetTBServices("Models/SeedData/tbservices.csv");
        }

        private static List<object> GetPHECtoLA()
        {
            return SeedingHelper.GetLAtoPHEC("Models/SeedData/LA_to_PHEC.csv");
        }

        private static List<object> GetHospitalsList()
        {
            return SeedingHelper.GetHospitalsList("Models/SeedData/hospitals.csv");
        }

        private static List<object> GetPHECList()
        {
            return SeedingHelper.GetPHECList("Models/SeedData/phec.csv");
        }

        private static List<object> GetLocalAuthoritiesList()
        {
            return SeedingHelper.GetLocalAuthorities("Models/SeedData/LocalAuthorities.csv");
        }
    }
}
