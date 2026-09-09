using System.IO;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Systems
{
    /// <summary>
    /// 세이브/로드 — 원본 `saveMeta()`/`loadMeta()`(project_test.html:1213·:1216).
    /// 원본은 localStorage에 `meta`를 통째로 JSON으로 넣는다. 우리도 같은 구조로
    /// <see cref="PlayerProfile"/> 하나를 파일로 쓴다.
    ///
    /// CLAUDE.md 폴더 규칙: **데이터 형태는 `Core`, 실제 파일 입출력은 `Systems`.**
    /// 그래서 `PlayerProfile`은 Core에 있고 읽고 쓰는 건 여기다.
    ///
    /// ## 기본값 위에 덮어쓰는 이유
    /// 원본 `loadMeta`는 `Object.assign(defaultMeta(), d)`로 **기본값에서 시작해 저장분을 덮는다**
    /// (`:1219`). 그래야 나중에 필드가 추가돼도 옛 세이브가 그대로 열린다 — 없는 필드는 기본값이 남는다.
    /// `JsonUtility.FromJsonOverwrite`가 정확히 같은 동작이라 그대로 썼다.
    /// 통째로 역직렬화(`FromJson`)하면 옛 세이브에서 새 필드가 0/null로 죽는다.
    ///
    /// ## 자동 저장은 여기 없다
    /// 이 클래스는 **부르면 하는 일만** 한다. 자동 저장/로드는 <see cref="AutoSave"/>가 맡는다 —
    /// 그래야 PlayMode 테스트가 실제 세이브 파일을 건드리지 않는다(테스트는 이 컴포넌트를 안 붙인다).
    /// </summary>
    public static class SaveService
    {
        const string FileName = "profile.json";

        /// <summary>
        /// 저장 위치. `Application.persistentDataPath`는 플랫폼마다 다른 "이 앱의 저장 폴더"다
        /// (에디터/윈도우는 `%USERPROFILE%/AppData/LocalLow/<회사>/<제품>`).
        /// 프로젝트 폴더에 쓰면 git에 섞이고 빌드본에선 쓰기 권한이 없을 수 있다.
        /// </summary>
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave => File.Exists(SavePath);

        /// <summary>현재 프로필을 파일로 쓴다. 실패해도 게임은 계속 돌아야 하므로 삼키고 경고만 남긴다.</summary>
        public static bool Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(ProfileService.Current, true));
                return true;
            }
            catch (System.Exception e)
            {
                // 원본도 `try { ... } catch (e) {}`로 조용히 넘어간다(:1214) — 저장 실패로 게임이 멈추면 안 된다.
                Debug.LogWarning($"[SaveService] 저장 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>저장분을 읽어 <see cref="ProfileService.Current"/>를 교체한다. 파일이 없으면 false.</summary>
        public static bool Load()
        {
            if (!HasSave) return false;
            try
            {
                var profile = new PlayerProfile(); // 기본값에서 시작 — 위 "기본값 위에 덮어쓰는 이유" 참고
                JsonUtility.FromJsonOverwrite(File.ReadAllText(SavePath), profile);
                ProfileService.Current = profile;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveService] 불러오기 실패, 새 프로필로 시작: {e.Message}");
                return false;
            }
        }

        /// <summary>전체 초기화(원본 로비의 "전체 초기화" 버튼, project_test.html:524).</summary>
        public static void DeleteSave()
        {
            try { if (HasSave) File.Delete(SavePath); }
            catch (System.Exception e) { Debug.LogWarning($"[SaveService] 삭제 실패: {e.Message}"); }
            ProfileService.Reset();
        }
    }
}
