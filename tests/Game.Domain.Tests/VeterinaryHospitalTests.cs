using System.Linq;
using Game.Domain.Monsters;
using Xunit;

namespace Game.Domain.Tests
{
    public sealed class VeterinaryHospitalTests
    {
        static MonsterDefinition Def() => new MonsterDefinition("capture", "Capture", MonsterElement.Fire,
            MonsterRole.Dps, new MonsterStats(100, 10, 5, 1, 0.05), new MonsterStats(0, 0, 0, 0, 0));
        static Trainer MakeTrainer(int id)
        {
            var t = new Trainer { Id = id, Rarity = Rarity.Common, Personality = Personality.Capitalist };
            t.Roster.Add(Monster.Create(new MonsterId("starter_" + id), Def(), Rarity.Common,
                MonsterIvGrade.B, 1, id));
            return t;
        }

        [Fact]
        public void CapturedRecoveryAdmitsHundredthAndRejectsOneHundredAndFirstUntilBedOpens()
        {
            var trainer = MakeTrainer(0);
            var hospital = new VeterinaryHospital(new[] { trainer }, new VeterinaryHospitalConfig(recoveryBeds: 100));
            var admissions = Enumerable.Range(0, 100).Select(i => hospital.AdmitCaptured(
                Monster.Create(new MonsterId("wild_" + i), Def(), Rarity.Rare, MonsterIvGrade.B, 1, i), 0, 10)).ToArray();
            Assert.All(admissions, x => Assert.True(x.Accepted));
            Assert.Equal(100, hospital.OccupiedRecoveryBeds);
            var overflow = Monster.Create(new MonsterId("wild_overflow"), Def(), Rarity.Rare, MonsterIvGrade.B, 1, 101);
            Assert.Equal(AdmissionStatus.Full, hospital.AdmitCaptured(overflow, 0, 10).Status);
            Assert.Equal(MonsterCustody.Unassigned, overflow.Custody);
            var bank = new GeneBank(new[] { trainer });
            var banked = Monster.Create(new MonsterId("wild_banked"), Def(), Rarity.Rare, MonsterIvGrade.B, 1, 102);
            Assert.True(bank.StoreUnassignedMonster(0, banked));
            Assert.Equal(MonsterCustody.GeneBank, banked.Custody);
            Assert.True(hospital.CompleteRecovery(admissions[0].RecoveryId).Accepted);
            Assert.True(hospital.AdmitCaptured(overflow, 0, 10).Accepted);
            Assert.Equal(MonsterCustody.Hospital, overflow.Custody);
        }

        [Fact]
        public void EmergencyCareChargesAndTakesLongerForFaintedMonster()
        {
            var trainer = MakeTrainer(0);
            var monster = trainer.Roster.Active;
            var hospital = new VeterinaryHospital(new[] { trainer }, new VeterinaryHospitalConfig());
            monster.SetCurrentHp(0);
            var fainted = hospital.RequestEmergencyCare(monster.Id, 100);
            Assert.True(fainted.Accepted);
            Assert.Equal(190, fainted.Fee); // 10 units x 14 Gold + 50 surcharge
            Assert.Equal(180, fainted.CompleteAtMinute - 100); // 60 x 3 configured recovery multiplier
            Assert.Equal(0, monster.CurrentHp);
            Assert.Equal(MonsterLifeState.Recovering, monster.LifeState);
            Assert.Equal(AdmissionStatus.AlreadyAdmitted, hospital.RequestEmergencyCare(monster.Id, 110).Status);
            Assert.Throws<System.InvalidOperationException>(() => trainer.Roster.MoveToStorage(monster.Id));
            Assert.True(hospital.CompleteRecovery(fainted.RecoveryId).Accepted);
            Assert.Equal(monster.MaxHp, monster.CurrentHp);
            Assert.Equal(AdmissionStatus.NoMissingHp, hospital.RequestEmergencyCare(monster.Id, 400).Status);
        }

        [Fact]
        public void HospitalServiceQuoteAddsConfiguredFaintSurchargeAndRecoveryMultiplier()
        {
            var building = new ServiceBuilding(new BuildingSpec(BuildingKind.Hospital, 14, 60), 1, 0);
            var profile = PersonalityProfile.Of(Personality.Warlike);
            Assert.Equal(190, building.PriceFor(profile, missingHp: 100, faintedCount: 1, faintedSurcharge: 50));
            Assert.Equal(180, building.ServiceMinutesFor(100, durationMultiplier: 3));
        }

        [Fact]
        public void CapturedMonsterRecoversForFirstCaptureDurationAndEntersTeamOrStorage()
        {
            var trainer = MakeTrainer(0);
            var hospital = new VeterinaryHospital(new[] { trainer }, new VeterinaryHospitalConfig(firstCaptureRecoveryMinutes: 720));
            var captured = Monster.Create(new MonsterId("captured"), Def(), Rarity.Epic, MonsterIvGrade.A, 3, 7);
            captured.SetCurrentHp(0);
            var admission = hospital.AdmitCaptured(captured, 0, 20);
            Assert.Equal(740, admission.CompleteAtMinute);
            Assert.Equal(MonsterCustody.Hospital, captured.Custody);
            Assert.Equal(MonsterLifeState.Recovering, captured.LifeState);
            hospital.CompleteRecovery(admission.RecoveryId);
            Assert.Equal(MonsterLifeState.Ready, captured.LifeState);
            Assert.Contains(trainer.Roster.Members, x => x.Id == captured.Id);

            var other = Monster.Create(new MonsterId("captured_2"), Def(), Rarity.Epic, MonsterIvGrade.A, 3, 8);
            var otherAdmission = hospital.AdmitCaptured(other, 0, 20);
            hospital.CompleteRecovery(otherAdmission.RecoveryId);
            var third = Monster.Create(new MonsterId("captured_3"), Def(), Rarity.Epic, MonsterIvGrade.A, 3, 9);
            var thirdAdmission = hospital.AdmitCaptured(third, 0, 20);
            hospital.CompleteRecovery(thirdAdmission.RecoveryId);
            Assert.Contains(trainer.Roster.Storage, x => x.Id == third.Id);
        }
    }
}
