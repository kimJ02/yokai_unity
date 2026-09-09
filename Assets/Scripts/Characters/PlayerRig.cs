using System.Collections.Generic;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 플레이어 오브젝트에 붙은 캐릭터 키트들 중 **선택된 하나만** 켠다.
    /// 원본 `updatePlayer`가 `meta.weapon`으로 갱신 함수를 갈라 부르는 것(project_test.html:3509~3538)에
    /// 대응한다 — 원본은 함수 분기지만 Unity는 컴포넌트마다 `Update()`가 따로 도니, 안 쓰는 키트를
    /// 꺼주지 않으면 네 캐릭터가 동시에 공격하게 된다.
    ///
    /// 전환 시 하는 일:
    /// 1. 이전 키트 `OnDeselected()` → 비활성화
    /// 2. `PlayerProfile.character` 갱신(파생 스탯의 캐릭터 배수가 이 값을 본다)
    /// 3. 이동 방식을 새 캐릭터에 맞춤(<see cref="ICharacterKit.RequiredMoveMode"/>) + 관성 속도 초기화
    /// 4. 새 키트 활성화 → `OnSelected()`
    ///
    /// 키트를 새로 만들면(섬영·드루이드) 이 오브젝트에 컴포넌트로 붙이기만 하면 자동으로 등록된다 —
    /// 이 파일을 고칠 필요가 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerRig : MonoBehaviour
    {
        /// <summary>지금 선택된 캐릭터. 초기값은 원본 `defaultMeta().character`(:1134)와 같은 마법사.</summary>
        public CharacterId Current { get; private set; } = CharacterId.Mage;

        /// <summary>이 오브젝트에 실제로 붙어 있는 키트들. 없는 캐릭터는 전환이 거부된다.</summary>
        public IEnumerable<CharacterId> AvailableCharacters
        {
            get
            {
                foreach (var kit in kits) yield return kit.Character;
            }
        }

        readonly List<ICharacterKit> kits = new List<ICharacterKit>();
        CharacterMover2D mover;

        void Awake()
        {
            mover = GetComponent<CharacterMover2D>();
            kits.Clear();
            kits.AddRange(GetComponents<ICharacterKit>());

            // 프로필이 이미 다른 캐릭터를 가리키고 있으면(전환 후 씬 재시작 등) 그쪽을 따른다.
            var wanted = ProfileService.Current.character;
            Current = HasKit(wanted) ? wanted : CharacterId.Mage;
            ApplySelection(Current, notifyPrevious: false);
        }

        public bool HasKit(CharacterId id)
        {
            foreach (var kit in kits)
                if (kit.Character == id) return true;
            return false;
        }

        /// <summary>
        /// 캐릭터를 바꾼다. 해당 키트가 이 오브젝트에 없으면 아무것도 안 하고 false를 돌려준다
        /// (아직 구현 안 된 캐릭터로 전환을 시도하는 경우 — 0차 단계에선 정상 상황이다).
        /// </summary>
        public bool Select(CharacterId id)
        {
            if (!HasKit(id)) return false;
            if (id == Current && Application.isPlaying) return true;
            ApplySelection(id, notifyPrevious: true);
            return true;
        }

        /// <summary>구현된 다음 캐릭터로 순환 전환(디버그 조작용).</summary>
        public bool SelectNext()
        {
            var order = new[] { CharacterId.Mage, CharacterId.Gunner, CharacterId.Blade, CharacterId.Druid };
            int start = System.Array.IndexOf(order, Current);
            for (int step = 1; step <= order.Length; step++)
            {
                var candidate = order[(start + step) % order.Length];
                if (HasKit(candidate)) return Select(candidate);
            }
            return false;
        }

        void ApplySelection(CharacterId id, bool notifyPrevious)
        {
            foreach (var kit in kits)
            {
                bool selected = kit.Character == id;
                var behaviour = kit as MonoBehaviour;

                if (!selected)
                {
                    if (notifyPrevious && kit.Character == Current) kit.OnDeselected();
                    if (behaviour != null) behaviour.enabled = false;
                }
            }

            Current = id;
            ProfileService.Current.character = id;

            foreach (var kit in kits)
            {
                if (kit.Character != id) continue;

                // 이동 방식을 먼저 맞추고 관성 속도를 버린 뒤에 키트를 켠다 — 키트가 OnSelected에서
                // MoveScale 같은 값을 세팅할 수 있으므로 순서를 뒤집으면 그게 지워진다.
                if (mover != null)
                {
                    mover.ResetMotion();
                    mover.moveMode = kit.RequiredMoveMode;
                }

                if (kit is MonoBehaviour behaviour) behaviour.enabled = true;
                kit.OnSelected();
                break;
            }
        }
    }
}
