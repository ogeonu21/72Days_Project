using UnityEngine;

[CreateAssetMenu(menuName="72Days/Loading Settings")]
public sealed class LoadingSettings : ScriptableObject
{
    public string dataEndpoint="https://script.google.com/macros/s/AKfycbzd47uZXwCe472lYE247EzI4jabj9fhZOIRgXUVL6f74683LiMpo7T4NnMFDc2G76VG-Q/exec";
    public string reportEndpoint="";
    public string mainScene="MainWindow";
    public int timeoutSeconds=30;
    public int maximumResponseBytes=4000000;
    public float minimumLoadingSeconds=2;
}
