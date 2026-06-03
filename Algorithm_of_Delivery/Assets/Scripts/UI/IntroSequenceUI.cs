using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AlgorithmOfDelivery.UI
{
    public class IntroSequenceUI : MonoBehaviour
    {
        [SerializeField, TextArea(12, 40)]
        private string _message =
            "육지에서 출발한 정기선이 가장 마지막에 닻을 내리는 곳. 지도를 아무리 펼쳐보아도 작은 점 하나로만 기록된 이 섬은, 거친 해안선과 깎아지른 절벽에 갇혀 제각각의 시간을 살아가고 있습니다.\n\n"
            + "이곳의 사람들은 서로 가까이 살면서도 멀리 떨어져 있습니다. 해안가 비좁은 골목길에 옹기종기 모여 앉은 이들부터, 산비탈 층층이 테라스를 쌓고 구름 아래 터를 잡은 노인들까지. 그들은 각자의 침묵 속에서 바다 건너 들려올 누군가의 목소리를 평생토록 기다리며 살아갈지도 모릅니다.\n\n\n\n\n"
            + "언제부터인가 섬의 고립은 깊어졌습니다. 선착장 관리인은 텅 빈 하역장을 보며 한숨을 내쉬고, 시장가의 활기는 빛바랜 간판처럼 서서히 식어갔죠.\n\n"
            + "섬의 북쪽 끝, 과거의 흔적을 쫓아 모여든 학자들은 유적 아래 묻힌 목소리를 캐내려 애쓰지만, 정작 현재를 살아가는 이들의 이야기는 갈 곳을 잃고 우체국 구석에 먼지처럼 쌓여만 갔습니다. 섬은 살아있으나, 사람과 사람을 잇는 혈관은 굳어버린 채 멈춰 서 있었습니다.\n\n\n\n\n"
            + "당신이 오늘 이 낡은 우체국의 문을 열었을 때, 섬은 비로소 긴 잠에서 깨어나기 시작한 것이었습니다.\n\n"
            + "당신은 이곳에서 단순히 물건을 옮기는 자가 아닙니다. 험로를 뚫고 산등성이를 넘어 육지의 소식을 전하고, 고립을 견딜 인사를 건네야 하는 유일한 연결자입니다. 발을 딛는 곳마다 묻어나는 비릿한 바다 내음과 가파른 오르막길의 거친 숨소리가 이제 당신의 일상이 될 것입니다.\n\n"
            + "안개가 걷히기 시작하는 항구마을의 아침. 이제, 멈춰있던 섬의 이야기가 당신의 발걸음 끝에서 다시 써지려 합니다.";

        [SerializeField] private float _duration = 15f;
        [SerializeField] private Vector2 _startOffset = new Vector2(0f, 760f);
        [SerializeField] private Vector2 _endOffset = new Vector2(0f, -300f);

        private RectTransform _textRect;
        private Text _text;
        private Coroutine _playRoutine;
        private Action _onComplete;

        public void Initialize(Text text)
        {
            _text = text;
            _textRect = text != null ? text.rectTransform : null;
            gameObject.SetActive(false);
        }

        public void Play(Action onComplete = null)
        {
            _onComplete = onComplete;

            if (_playRoutine != null)
                StopCoroutine(_playRoutine);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        public void Hide()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _onComplete = null;
            gameObject.SetActive(false);
        }

        private IEnumerator PlayRoutine()
        {
            if (_text != null)
                _text.text = _message;

            if (_textRect != null)
                _textRect.anchoredPosition = _startOffset;

            float duration = Mathf.Max(0.01f, _duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                if (_textRect != null)
                    _textRect.anchoredPosition = Vector2.Lerp(_startOffset, _endOffset, t);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_textRect != null)
                _textRect.anchoredPosition = _endOffset;

            _playRoutine = null;

            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }
    }
}
