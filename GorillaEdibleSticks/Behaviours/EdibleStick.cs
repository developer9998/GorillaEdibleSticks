using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GorillaEdibleSticks.Behaviours
{
    [RequireComponent(typeof(TransferrableObject), typeof(MeshRenderer))]
    public class EdibleStick : MonoBehaviour
    {
        // Assets
        private static bool _bundleLoaded;
        private static AssetBundle _storedBundle;

        private static Task _loadingTask = null;
        private static readonly Dictionary<string, Object> _assetCache = new();

        // Physical Object/State
        private GameObject _edibleStick;
        private int _edibleState, _lastState;

        // Logic
        private Vector3 _biteOffset = new Vector3(0f, 0.0208f, 0.171f);
        private float _biteDistance = 0.1666667f;

        private readonly float _respawnTime = 7f, _eatCooldown = 1f;

        private float _lastEatTime, _lastEatTime_Fully;

        private Transform _eatSpot;
        private bool _inBiteSpot;

        private static async Task LoadBundle()
        {
            var taskCompletionSource = new TaskCompletionSource<AssetBundle>();

            Stream str = typeof(Plugin).Assembly.GetManifestResourceStream("GorillaEdibleSticks.Content.ediblestickbundle");
            var request = AssetBundle.LoadFromStreamAsync(str);

            request.completed += operation =>
            {
                var outRequest = operation as AssetBundleCreateRequest;
                taskCompletionSource.SetResult(outRequest.assetBundle);
            };

            _storedBundle = await taskCompletionSource.Task;
            _bundleLoaded = true;
        }

        public static async Task<T> LoadAsset<T>(string name) where T : Object
        {
            if (_assetCache.TryGetValue(name, out var _loadedObject)) return _loadedObject as T;

            if (!_bundleLoaded)
            {
                _loadingTask ??= LoadBundle();
                await _loadingTask;
            }

            var taskCompletionSource = new TaskCompletionSource<T>();
            var request = _storedBundle.LoadAssetAsync<T>(name);

            request.completed += operation =>
            {
                var outRequest = operation as AssetBundleRequest;
                if (outRequest.asset == null)
                {
                    taskCompletionSource.SetResult(null);
                    return;
                }

                taskCompletionSource.SetResult(outRequest.asset as T);
            };

            var _finishedTask = await taskCompletionSource.Task;
            if (!_assetCache.ContainsKey(name)) _assetCache.Add(name, _finishedTask);
            return _finishedTask;
        }

        public async void Awake()
        {
            GetComponent<MeshRenderer>().forceRenderingOff = true;

            _edibleStick = Instantiate(await LoadAsset<GameObject>("EdibleStick Parent"), transform);
            _edibleStick.transform.localPosition = Vector3.zero;
            _edibleStick.transform.localEulerAngles = Vector3.zero;
            _edibleStick.transform.localScale = Vector3.one;

            _eatSpot = _edibleStick.transform.Find("EatSpot");
        }

        public void LateUpdate()
        {
            if (_edibleState == 3 && Time.realtimeSinceStartup > _lastEatTime_Fully + _respawnTime)
            {
                _edibleState = 0;
                return;
            }
            else if (Time.realtimeSinceStartup > _lastEatTime + _eatCooldown)
            {
                bool isValid = false, inRange = false;
                float adjustedDistance = Mathf.Pow(_biteDistance, 2);

                if (GorillaParent.hasInstance)
                {
                    for (int i = 0; i < GorillaParent.instance.vrrigs.Count; i++)
                    {
                        VRRig vrrig = GorillaParent.instance.vrrigs[i];
                        if (!vrrig.isOfflineVRRig)
                        {
                            if (vrrig.head == null || vrrig.head.rigTarget == null) break;

                            Transform otherPlayerHead = vrrig.head.rigTarget.transform;
                            if ((otherPlayerHead.position + otherPlayerHead.rotation * _biteOffset - _eatSpot.position).sqrMagnitude < adjustedDistance) isValid = true;
                        }
                    }

                    Transform localPlayerHead = GorillaTagger.Instance.offlineVRRig.head.rigTarget.transform;
                    if ((localPlayerHead.position + localPlayerHead.rotation * _biteOffset - _eatSpot.position).sqrMagnitude < adjustedDistance)
                    {
                        isValid = true;
                        inRange = true;
                    }

                    if (isValid && !_inBiteSpot && (!inRange || GetComponent<TransferrableObject>().InHand()) && _edibleState != 3)
                    {
                        _edibleState = Mathf.Clamp(_edibleState + 1, 0, 3);
                        _lastEatTime = Time.realtimeSinceStartup;
                        _lastEatTime_Fully = Time.realtimeSinceStartup;
                    }

                    _inBiteSpot = isValid;
                }
            }
        }

        public void FixedUpdate()
        {
            if (_lastState != _edibleState)
            {
                _lastState = _edibleState;

                int _disabledState = ((_edibleState - 1) < 0 ? 4 : _edibleState) - 1;
                _edibleStick.transform.Find(string.Concat("Stick", _disabledState)).gameObject.SetActive(false);
                _edibleStick.transform.Find(string.Concat("Stick", _edibleState)).gameObject.SetActive(true);

                _eatSpot.GetComponent<AudioSource>().PlayOneShot(GorillaLocomotion.Player.Instance.materialData[_edibleState == 0 ? 84 : 84 + _edibleState].audio, 0.08f);
                if (GetComponent<TransferrableObject>().IsMyItem())
                {
                    float amplitude = GorillaTagger.Instance.tapHapticStrength / 4f;
                    float fixedDeltaTime = Time.fixedDeltaTime;

                    if (GetComponent<TransferrableObject>().InHand())
                    {
                        GorillaTagger.Instance.StartVibration(GetComponent<TransferrableObject>().InLeftHand(), amplitude, fixedDeltaTime);
                        return;
                    }

                    GorillaTagger.Instance.StartVibration(false, amplitude, fixedDeltaTime);
                    GorillaTagger.Instance.StartVibration(true, amplitude, fixedDeltaTime);
                }
            }
        }
    }
}
