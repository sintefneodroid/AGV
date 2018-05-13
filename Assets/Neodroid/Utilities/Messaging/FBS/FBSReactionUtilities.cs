using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neodroid.Utilities.Messaging.FBS {
  public static class FbsReactionUtilities {
    #region PublicMethods

    public static Messages.Reaction unpack_reaction(FReaction? reaction) {
      if (reaction.HasValue) {
        var motions = unpack_motions(reaction.Value);
        var configurations = unpack_configurations(reaction.Value);
        var displayables = unpack_displayables(reaction.Value);
        var unobservables = unpack_unobservables(reaction.Value);
        var parameters = unpack_parameters(reaction.Value);
        var serialised_message = unpack_serialised_message(reaction.Value);
        return new Messages.Reaction(
            parameters,
            motions,
            configurations,
            unobservables,
            displayables,
            serialised_message);
      }

      return new Messages.Reaction(null, null, null, null, null, "");
    }

    #endregion

    #region PrivateMethods

    static String unpack_serialised_message(FReaction reaction_value) {
      return reaction_value.SerialisedMessage;
    }

    static Messages.Unobservables unpack_unobservables(FReaction reaction) {
      if (reaction.Unobservables.HasValue) {
        var bodies = create_bodies(reaction.Unobservables.Value);

        var poses = create_poses(reaction.Unobservables.Value);

        return new Messages.Unobservables(bodies, poses);
      }

      return new Messages.Unobservables();
    }

    static Messages.ReactionParameters unpack_parameters(FReaction reaction) {
      if (reaction.Parameters.HasValue) {
        return new Messages.ReactionParameters(
            reaction.Parameters.Value.Terminable,
            reaction.Parameters.Value.Step,
            reaction.Parameters.Value.Reset,
            reaction.Parameters.Value.Configure,
            reaction.Parameters.Value.Describe,
            reaction.Parameters.Value.EpisodeCount);
      }

      return new Messages.ReactionParameters();
    }

    static Messages.Configuration[] unpack_configurations(FReaction reaction) {
      var l = reaction.ConfigurationsLength;
      var configurations = new Messages.Configuration[l];
      for (var i = 0; i < l; i++) {
        configurations[i] = create_configuration(reaction.Configurations(i));
      }

      return configurations;
    }

    static Messages.Displayables.Displayable[] unpack_displayables(FReaction reaction) {
      var l = reaction.DisplayablesLength;
      var configurations = new Messages.Displayables.Displayable[l];
      for (var i = 0; i < l; i++) {
        configurations[i] = create_displayable(reaction.Displayables(i));
      }

      return configurations;
    }

    static Messages.Displayables.Displayable create_displayable(FDisplayable? displayable) {
      if (displayable.HasValue) {
        var d = displayable.Value;

        switch (d.DisplayableValueType) {
          case FDisplayableValue.NONE: break;

          case FDisplayableValue.FValue:
            return new Messages.Displayables.DisplayableFloat(
                d.DisplayableName,
                d.DisplayableValue<FValue>()?.Val);

          case FDisplayableValue.FValues:
            var v3 = d.DisplayableValue<FValues>().GetValueOrDefault();
            var a1 = new List<float>();
            for (var i = 0; i < v3.ValsLength; i++) {
              a1.Add((float)v3.Vals(i));
            }

            return new Messages.Displayables.DisplayableValues(d.DisplayableName, a1.ToArray());

          case FDisplayableValue.FVector3s:
            var v2 = d.DisplayableValue<FVector3s>().GetValueOrDefault();
            var a = new List<Vector3>();
            for (var i = 0; i < v2.PointsLength; i++) {
              var p = v2.Points(i).GetValueOrDefault();
              var v = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
              a.Add(v);
            }

            return new Messages.Displayables.DisplayableVector3S(d.DisplayableName, a.ToArray());

          case FDisplayableValue.FValuedVector3s:
            var flat_fvec3 = d.DisplayableValue<FValuedVector3s>().GetValueOrDefault();
            var output = new List<Structs.Points.ValuePoint>();

            for (var i = 0; i < flat_fvec3.PointsLength; i++) {
              var val = (float)flat_fvec3.Vals(i);
              var p = flat_fvec3.Points(i).GetValueOrDefault();
              var v = new Structs.Points.ValuePoint(
                  new Vector3((float)p.X, (float)p.Y, (float)p.Z),
                  val,
                  1);
              output.Add(v);
            }

            return new Messages.Displayables.DisplayableValuedVector3S(d.DisplayableName, output.ToArray());

          case FDisplayableValue.FString:
            return new Messages.Displayables.DisplayableString(
                d.DisplayableName,
                d.DisplayableValue<FString>()?.Str);

          case FDisplayableValue.FByteArray: break;
          default: throw new ArgumentOutOfRangeException();
        }
      }

      return null;
    }

    static Messages.MotorMotion[] unpack_motions(FReaction reaction) {
      var l = reaction.MotionsLength;
      var motions = new Messages.MotorMotion[l];
      for (var i = 0; i < l; i++) {
        motions[i] = create_motion(reaction.Motions(i));
      }

      return motions;
    }

    static Messages.Configuration create_configuration(FConfiguration? configuration) {
      if (configuration.HasValue) {
        return new Messages.Configuration(
            configuration.Value.ConfigurableName,
            (float)configuration.Value.ConfigurableValue);
      }

      return null;
    }

    static Messages.MotorMotion create_motion(FMotion? motion) {
      if (motion.HasValue) {
        return new Messages.MotorMotion(
            motion.Value.ActorName,
            motion.Value.MotorName,
            (float)motion.Value.Strength);
      }

      return null;
    }

    static Pose[] create_poses(FUnobservables unobservables) {
      var l = unobservables.PosesLength;
      var poses = new Pose[l];
      for (var i = 0; i < l; i++) {
        poses[i] = create_pose(unobservables.Poses(i));
      }

      return poses;
    }

    static Messages.Body[] create_bodies(FUnobservables unobservables) {
      var l = unobservables.BodiesLength;
      var bodies = new Messages.Body[l];
      for (var i = 0; i < l; i++) {
        bodies[i] = create_body(unobservables.Bodies(i));
      }

      return bodies;
    }

    static Pose create_pose(FQuaternionTransform? trans) {
      if (trans.HasValue) {
        var position = trans.Value.Position;
        var rotation = trans.Value.Rotation;
        var vec3_pos = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
        var quat_rot = new Quaternion(
            (float)rotation.X,
            (float)rotation.Y,
            (float)rotation.Z,
            (float)rotation.W);
        return new Pose(vec3_pos, quat_rot);
      }

      return new Pose();
    }

    static Messages.Body create_body(FBody? body) {
      if (body.HasValue) {
        var vel = body.Value.Velocity;
        var ang = body.Value.AngularVelocity;
        var vec3_vel = new Vector3((float)vel.X, (float)vel.Y, (float)vel.Z);
        var vec3_ang = new Vector3((float)ang.X, (float)ang.Y, (float)ang.Z);
        return new Messages.Body(vec3_vel, vec3_ang);
      }

      return null;
    }

    #endregion
  }
}
