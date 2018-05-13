using UnityEngine;

namespace Neodroid.Utilities.Unsorted {
  /// <inheritdoc />
  /// <summary>
  /// </summary>
  public class GameObjectCloner : MonoBehaviour {
    [SerializeField] GameObject _prefab;
    [SerializeField] int _num_clones;
    [SerializeField] Vector3 _initial_offset = new Vector3(20, 0);
    [SerializeField] Vector3 _offset = new Vector3(20, 0, 20);



    void Start() {
      var eid = 0;
      if (this._prefab) {
        var clone_coords = NeodroidUtilities.SnakeSpaceFillingGenerator(this._num_clones);
        foreach (var clone_coord in clone_coords) {
          var go = Instantiate(
              this._prefab,
              this._initial_offset + Vector3.Scale(this._offset, clone_coord),
              Quaternion.identity,
              this.transform);
          go.name = $"{go.name}{eid++}";
        }
      }
    }
  }
}
