# 첫 화면 / Main Menu

`Assets/Scenes/MainMenu.unity`를 열고 Play하면 시작 화면이 나옵니다. 빌드 Scene List에서도 첫 번째 씬으로 등록되어 있습니다. REDACT 씬에서 바로 Play하면 기존처럼 튜토리얼 테스트를 할 수 있습니다.

- 배경: 기존 `Assets/UI.png`를 그대로 참조합니다. 원본 이미지 및 Import 설정은 변경하지 않습니다.
- 게임 시작: 기존 `Assets/Scenes/REDACT.unity` 튜토리얼 씬을 엽니다. 도감 저장 기록을 임의로 초기화하지 않습니다.
- 설정: 화면 해상도, 창/전체 화면, 현재 키 안내, 전체 음량/음소거.
- 게임 종료: 빌드에서는 종료, 에디터에서는 Play만 종료합니다.

## 직접 편집

### 투명 시작 버튼

시작 화면의 게임 시작 / 설정 / 게임 종료는 사각형 배경 없이 글자만 표시합니다. 마우스 또는 방향키로 선택하면 양옆 장식과 글자 확대/색상 강조가 나타나며, 클릭/Enter 입력 시 밝게 강조한 뒤 약 0.18초 후 동작합니다. 연속 입력은 한 번만 처리합니다. 설정 창 안의 조절 버튼은 기존 스타일을 유지합니다.

각 버튼의 `MenuButtonFeedback`에서 Normal/Selected/Clicked Color, Selected Scale, Click Duration을 조정할 수 있습니다. 양옆 장식은 `Selection Ornaments` 자식에서 위치/모양을 수정합니다. 버튼의 Image는 투명하지만 클릭 영역을 위해 유지하세요. 글자 Shadow는 밝은 배경에서도 읽히도록 추가했습니다.

Hierarchy의 `Main Menu Canvas` 안에서 UI를 수정하세요.

- `Background (UI.png)`: RawImage.Texture에 다른 배경을 연결할 수 있습니다. Aspect Ratio Fitter가 비율을 유지하며 화면을 채웁니다. 이미지 비율을 바꾸면 Aspect Ratio도 맞춰주세요. 화면 비율에 따라 가장자리가 잘릴 수 있습니다.
- `Main Buttons`: 게임 시작 / 설정 / 게임 종료 버튼의 Rect Transform, Image 색상, 자식 Text를 조정합니다.
- `Settings Overlay`: 편집할 때 활성화하여 `Settings Window`의 탭과 페이지를 조정할 수 있습니다. 실행 시에는 자동으로 숨겨집니다.
- `MainMenuController`: UI 참조와 Tutorial Scene을 연결합니다. 기존 참조를 삭제했다면 다시 연결하세요.
- `Korean Font`: 배포 가능한 한글 Font를 직접 넣을 수 있습니다. 비워두면 런타임에 맑은 고딕 등 OS 한글 폰트를 찾습니다. Windows에서 한글 표시를 검증했습니다. 다른 플랫폼 배포 시에는 사용 허가된 한글 폰트를 연결하세요. 에디터 비실행 시 기본 폰트로 한글이 안 보이면 이 설정은 실행 시 적용됩니다.
- UI 위치/크기는 실행 중 스크립트가 덮어쓰지 않습니다. Canvas Scaler 기준은 1280×720, Expand입니다.

## 설정 저장 / 화면 안전장치

### 튜토리얼 로딩

- `MainMenuController > Prioritize Scene Loading`을 기본으로 켰습니다. 게임 플레이 중이 아닌 씬 전환 중에만 `Application.backgroundLoadingPriority`를 High로 높입니다. 프레임당 로딩 처리 시간을 더 허용하므로 로딩 중 메뉴 프레임은 다소 낮아질 수 있습니다.
- `Internal/MenuSceneLoadOperation.cs`는 임시 런타임 로더입니다. 직접 부착하지 않습니다. 메뉴가 사라져도 씬 활성화 완료를 추적하고 이전 우선순위를 복원한 뒤 자신을 제거합니다. 종료/오류 때도 복원합니다.
- 별도 로딩 씬이나 인위적인 추가 대기 시간은 없습니다. 기존 클릭 강조 0.18초는 유지합니다. 화면의 진행 표시는 Unity의 씬 진행률을 바탕으로 하며 완료 전에는 100%로 표시하지 않습니다.
- Console에서 `[SceneLoad] 시작` / `[SceneLoad] 완료`를 검색하면 실제 씬 활성화를 포함한 소요 시간을 확인할 수 있습니다. 클릭 연출 시간은 이 수치에 포함하지 않습니다.
- 이미지 해상도/압축, 맵, 아틀라스, 플레이어와 튜토리얼 기능은 변경하지 않았습니다. 사전 로딩으로 대기 시간을 앞당기는 방식도 쓰지 않습니다.
- 테스트 복사본에서 첫 기본 로드 1.152초, 반복 기본 0.579초, 높은 우선순위 0.564/0.510초였습니다. 최초 리소스 캐시와 실행 조건이 다르므로 원본 로그의 18.047초가 이 수치로 줄어든다고 보장하지 않습니다. 에디터 첫 로딩과 빌드는 별도 측정해야 합니다.
- API 기준: https://docs.unity3d.com/kr/6000.0/ScriptReference/Application-backgroundLoadingPriority.html

- 화면 적용 → 15초 안에 유지 → 저장. 되돌리기, Esc, 제한 시간 만료, 설정 닫기는 이전 화면으로 복귀합니다.
- Unity 에디터에서는 실제 해상도를 변경하지 않습니다. Game View 비율/해상도는 에디터에서 직접 바꾸세요. 실제 디스플레이 전환은 PC 빌드에서 테스트합니다.
- 전체 화면은 테두리 없는 전체 화면입니다. 해상도 목록은 현재 모니터 목록 + 1280×720 / 1600×900 / 1920×1080 / 현재 창 크기이며 중복을 제거합니다.
- 소리는 AudioListener.volume으로 전체 출력을 제어합니다. BGM/SFX 분리나 키 재지정 기능은 포함하지 않습니다.
- `Redact.Settings.*` PlayerPrefs만 사용합니다. 기존 도감 등 다른 저장 데이터는 지우지 않습니다.
- Enter/방향키 또는 마우스로 버튼을 누를 수 있습니다. Esc는 설정 닫기/화면 변경 취소입니다. 게임 내 일시정지 UI를 추가하는 기능은 아닙니다.

## 폴더 역할

- `Attachable/MainMenuController.cs`: Canvas에 부착하는 메뉴 동작.
- `Internal/MenuPreferences.cs`: 별도 부착 없이 저장한 설정을 적용하는 정적 도우미.
- `Internal/Editor/MainMenuSceneBuilder.cs`: 메뉴 씬을 처음 만들 때 쓰는 에디터 전용 도구. 기존 씬은 덮어쓰지 않습니다. 자동 실행되지 않습니다.

빌드할 때 사용하는 커스텀 Build Profile에 별도 Scene List가 있다면 MainMenu를 첫 번째, REDACT를 그 다음에 직접 등록하세요. 튜토리얼 및 플레이어 스크립트는 이 작업에서 수정하지 않습니다.
