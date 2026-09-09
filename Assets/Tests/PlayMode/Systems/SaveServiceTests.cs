using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Core;
using YokaiFront.Systems;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 세이브/로드 — 원본 `saveMeta`/`loadMeta`(project_test.html:1213·:1216).
///
/// ⚠️ 이 테스트는 **실제 세이브 파일 경로를 쓴다.** 개발자가 플레이하며 쌓은 진행이 날아가지 않게
/// 시작 시 원본을 백업하고 끝나면 되돌린다. (자동 저장 컴포넌트 `AutoSave`는 씬 빌더만 붙이므로
/// 테스트가 붙이지 않는 한 실행되지 않는다.)
/// </summary>
public class SaveServiceTests
{
    string backup;
    bool hadSave;

    [SetUp]
    public void Setup()
    {
        hadSave = SaveService.HasSave;
        backup = hadSave ? File.ReadAllText(SaveService.SavePath) : null;
        ProfileService.Reset();
    }

    [TearDown]
    public void Teardown()
    {
        if (hadSave) File.WriteAllText(SaveService.SavePath, backup);
        else if (SaveService.HasSave) File.Delete(SaveService.SavePath);
        ProfileService.Reset();
    }

    /// <summary>저장했다가 다시 읽으면 진행이 그대로 살아나야 한다 — 이게 안 되면 로비 강화가 무의미하다.</summary>
    [Test]
    public void SaveThenLoad_RestoresProgress()
    {
        var p = ProfileService.Current;
        p.level = 7;
        p.exp = 123;
        p.gold = 4560;
        p.character = CharacterId.Gunner;
        p.mageBranch = MageBranch.Gravity;
        p.mageTier = 3;
        p.spUsed = 6;
        p.upgrades.atk = 11;
        p.upgrades.crit = 4;
        p.regionBossCleared[0] = true;

        Assert.IsTrue(SaveService.Save());

        ProfileService.Reset(); // 껐다 켠 셈
        Assert.AreEqual(1, ProfileService.Current.level);

        Assert.IsTrue(SaveService.Load());
        var q = ProfileService.Current;

        Assert.AreEqual(7, q.level);
        Assert.AreEqual(123, q.exp);
        Assert.AreEqual(4560, q.gold);
        Assert.AreEqual(CharacterId.Gunner, q.character);
        Assert.AreEqual(MageBranch.Gravity, q.mageBranch);
        Assert.AreEqual(3, q.mageTier);
        Assert.AreEqual(6, q.spUsed);
        Assert.AreEqual(11, q.upgrades.atk);
        Assert.AreEqual(4, q.upgrades.crit);
        Assert.IsTrue(q.regionBossCleared[0]);
    }

    /// <summary>
    /// **옛 세이브 호환** — 원본 `Object.assign(defaultMeta(), d)`(:1219)와 같은 동작인지 본다.
    /// 나중에 필드를 추가해도 기존 세이브가 열려야 하고, 없는 필드는 기본값이 남아야 한다.
    /// 통째로 역직렬화하면 이 경우 새 필드가 0/null로 죽는다.
    /// </summary>
    [Test]
    public void Load_FillsMissingFieldsWithDefaults()
    {
        // 아주 옛날 세이브인 척 — level/gold만 들어 있다.
        File.WriteAllText(SaveService.SavePath, "{\"level\":5,\"gold\":900}");

        Assert.IsTrue(SaveService.Load());
        var p = ProfileService.Current;

        Assert.AreEqual(5, p.level);
        Assert.AreEqual(900, p.gold);
        Assert.IsNotNull(p.upgrades, "저장에 없던 필드가 null로 죽었다 — 기본값 위에 덮어써야 한다");
        Assert.AreEqual(0, p.upgrades.atk);
        Assert.IsNotNull(p.regionBossCleared);
        Assert.AreEqual(MageBranch.None, p.mageBranch);
    }

    /// <summary>깨진 파일이어도 게임이 죽지 않고 새 프로필로 시작해야 한다(원본도 try/catch로 삼킨다).</summary>
    [Test]
    public void Load_SurvivesCorruptFile()
    {
        File.WriteAllText(SaveService.SavePath, "이건 JSON이 아니다 {{{");

        LogAssert.ignoreFailingMessages = true; // 경고 로그가 테스트를 실패시키지 않게
        bool ok = SaveService.Load();
        LogAssert.ignoreFailingMessages = false;

        Assert.IsFalse(ok, "깨진 파일을 성공으로 보고하면 안 된다");
        Assert.AreEqual(1, ProfileService.Current.level, "깨진 세이브를 읽고 프로필이 오염됐다");
    }

    /// <summary>세이브가 없으면 조용히 false — 첫 실행이 그 경우다.</summary>
    [Test]
    public void Load_ReturnsFalseWhenNoSave()
    {
        if (SaveService.HasSave) File.Delete(SaveService.SavePath);
        Assert.IsFalse(SaveService.Load());
    }

    /// <summary>전체 초기화(원본 로비 버튼 :524) — 파일도 지우고 메모리 프로필도 기본값으로.</summary>
    [Test]
    public void DeleteSave_ClearsFileAndProfile()
    {
        ProfileService.Current.gold = 9999;
        SaveService.Save();
        Assert.IsTrue(SaveService.HasSave);

        SaveService.DeleteSave();

        Assert.IsFalse(SaveService.HasSave);
        Assert.AreEqual(0, ProfileService.Current.gold);
    }
}

}
