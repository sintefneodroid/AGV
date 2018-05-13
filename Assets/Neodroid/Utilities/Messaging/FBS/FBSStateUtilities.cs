using System;
using System.Collections.Generic;
using FlatBuffers;
using Neodroid.Prototyping.Actors;
using Neodroid.Prototyping.Configurables;
using Neodroid.Prototyping.Motors;
using Neodroid.Prototyping.Observers;
using Neodroid.Utilities.Interfaces;
using UnityEngine;

namespace Neodroid.Utilities.Messaging.FBS {
  /// <summary>
  ///
  /// </summary>
  public static class FbsStateUtilities {
    #region PublicMethods

    /// <summary>
    ///
    /// </summary>
    /// <param name="states"></param>
    /// <returns></returns>
    public static byte[] build_states(IEnumerable<Messages.EnvironmentState> states) {
      var b = new FlatBufferBuilder(1);
      foreach (var state in states) {
        if (state != null) {
          var n = b.CreateString(state.EnvironmentName);

          var observables_vector = FState.CreateObservablesVector(b, state.Observables);

          var observers = new Offset<FOBS>[state.Observations.Values.Count];
          var k = 0;
          foreach (var observer in state.Observations.Values) {
            observers[k++] = build_observer(b, observer);
          }

          var observers_vector = FState.CreateObservationsVector(b, observers);

          FUnobservables.StartBodiesVector(b, state.Unobservables.Bodies.Length);
          foreach (var rig in state.Unobservables.Bodies) {
            var vel = rig.Velocity;
            var ang = rig.AngularVelocity;
            FBody.CreateFBody(b, vel.x, vel.y, vel.z, ang.x, ang.y, ang.z);
          }

          var bodies_vector = b.EndVector();

          FUnobservables.StartPosesVector(b, state.Unobservables.Poses.Length);
          foreach (var tra in state.Unobservables.Poses) {
            var pos = tra.position;
            var rot = tra.rotation;
            FQuaternionTransform.CreateFQuaternionTransform(
                b,
                pos.x,
                pos.y,
                pos.z,
                rot.x,
                rot.y,
                rot.z,
                rot.w);
          }

          var poses_vector = b.EndVector();

          FUnobservables.StartFUnobservables(b);
          FUnobservables.AddPoses(b, poses_vector);
          FUnobservables.AddBodies(b, bodies_vector);
          var unobservables = FUnobservables.EndFUnobservables(b);

          var description_offset = new Offset<FEnvironmentDescription>();
          if (state.Description != null) {
            description_offset = build_description(b, state);
          }

          var d = new StringOffset();
          if (state.DebugMessage != "") {
            d = b.CreateString(state.DebugMessage);
          }

          var t = b.CreateString(state.TerminationReason);

          FState.StartFState(b);
          FState.AddEnvironmentName(b, n);

          FState.AddFrameNumber(b, state.FrameNumber);
          FState.AddObservables(b, observables_vector);
          FState.AddUnobservables(b, unobservables);

          FState.AddTotalEnergySpent(b, state.TotalEnergySpentSinceReset);
          FState.AddSignal(b, state.Signal);

          FState.AddTerminated(b, state.Terminated);
          FState.AddTerminationReason(b, t);

          FState.AddObservations(b, observers_vector);
          if (state.Description != null) {
            FState.AddEnvironmentDescription(b, description_offset);
          }

          if (state.DebugMessage != "") {
            FState.AddSerialisedMessage(b, d);
          }

          var offset = FState.EndFState(b);

          FState.FinishFStateBuffer(b, offset);
        }
      }

      return b.SizedByteArray();
    }

    #endregion

    #region PrivateMethods

    static Offset<FMotor> build_motor(FlatBufferBuilder b, Motor motor, string identifier) {
      var n = b.CreateString(identifier);
      FMotor.StartFMotor(b);
      FMotor.AddMotorName(b, n);
      FMotor.AddValidInput(
          b,
          FRange.CreateFRange(
              b,
              motor.MotionValueSpace._Decimal_Granularity,
              motor.MotionValueSpace._Max_Value,
              motor.MotionValueSpace._Min_Value));
      FMotor.AddEnergySpentSinceReset(b, motor.GetEnergySpend());
      return FMotor.EndFMotor(b);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="b"></param>
    /// <param name="observer"></param>
    /// <returns></returns>
    static Offset<FEulerTransform> build_euler_transform(FlatBufferBuilder b, IHasEulerTransform observer) {
      Vector3 pos = observer.Position, rot = observer.Rotation, dir = observer.Direction;
      return FEulerTransform.CreateFEulerTransform(
          b,
          pos.x,
          pos.y,
          pos.z,
          rot.x,
          rot.y,
          rot.z,
          dir.x,
          dir.y,
          dir.z);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="b"></param>
    /// <param name="observer"></param>
    /// <returns></returns>
    static Offset<FQT> build_quaternion_transform(FlatBufferBuilder b, IHasQuaternionTransform observer) {
      var pos = observer.Position;
      var rot = observer.Rotation;
      FQT.StartFQT(b);
      FQT.AddTransform(
          b,
          FQuaternionTransform.CreateFQuaternionTransform(
              b,
              pos.x,
              pos.y,
              pos.z,
              rot.x,
              rot.y,
              rot.z,
              rot.w));
      return FQT.EndFQT(b);
    }

    static Offset<FByteArray> build_byte_array(FlatBufferBuilder b, IHasByteArray camera) {
      //var v_offset = FByteArray.CreateBytesVector(b, camera.Bytes);
      var v_offset = CustomFlatBufferImplementation.CreateByteVector(b, camera.Bytes);
      FByteArray.StartFByteArray(b);
      FByteArray.AddType(b, FByteDataType.PNG);
      FByteArray.AddBytes(b, v_offset);
      return FByteArray.EndFByteArray(b);
    }

    static Offset<FArray> build_array(FlatBufferBuilder b, IHasArray float_a) {
      //var v_offset = FArray.CreateArrayVector(b, camera.ObservationArray);
      var v_offset = CustomFlatBufferImplementation.CreateFloatVector(b, float_a.ObservationArray);
      //FArray.StartRangesVector(b,);
      FArray.StartFArray(b);
      FArray.AddArray(b, v_offset);
      //FArray.AddRanges(b,);
      return FArray.EndFArray(b);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="b"></param>
    /// <param name="vel"></param>
    /// <param name="ang"></param>
    /// <returns></returns>
    static Offset<FRB> build_body_observation(FlatBufferBuilder b, Vector3 vel, Vector3 ang) {
      FRB.StartFRB(b);
      FRB.AddBody(b, FBody.CreateFBody(b, vel.x, vel.y, vel.z, ang.x, ang.y, ang.z));
      return FRB.EndFRB(b);
    }

    static Offset<FSingle> build_single(FlatBufferBuilder b, IHasSingle numeral) {
      FSingle.StartFSingle(b);
      FSingle.AddValue(b, numeral.ObservationValue);

      var range_offset = FRange.CreateFRange(
          b,
          numeral.SingleSpace._Decimal_Granularity,
          numeral.SingleSpace._Max_Value,
          numeral.SingleSpace._Min_Value);
      FSingle.AddRange(b, range_offset);
      return FSingle.EndFSingle(b);
    }

    static Offset<FDouble> build_double(FlatBufferBuilder b, IHasDouble numeral) {
      FDouble.StartFDouble(b);
      var vec2 = numeral.ObservationValue;
      FDouble.AddVec2(b, FVector2.CreateFVector2(b, vec2.x, vec2.y));
      //FSingle.AddRange(b, numeral.ObservationValue);
      return FDouble.EndFDouble(b);
    }

    static Offset<FTriple> build_triple(FlatBufferBuilder b, IHasTriple numeral) {
      FTriple.StartFTriple(b);
      var vec3 = numeral.ObservationValue;
      FTriple.AddVec3(b, FVector3.CreateFVector3(b, vec3.x, vec3.y, vec3.z));
      //FSingle.AddRange(b, numeral.ObservationValue);
      return FTriple.EndFTriple(b);
    }

    static Offset<FQuadruple> build_quadruple(FlatBufferBuilder b, IHasQuadruple numeral) {
      FQuadruple.StartFQuadruple(b);
      var quad = numeral.ObservationValue;
      FQuadruple.AddQuat(b, FQuaternion.CreateFQuaternion(b, quad.x, quad.y, quad.z, quad.z));
      //FSingle.AddRange(b, numeral.ObservationValue);
      return FQuadruple.EndFQuadruple(b);
    }

    static Offset<FActor> build_actor(
        FlatBufferBuilder b,
        Offset<FMotor>[] motors,
        Actor actor,
        string identifier) {
      var n = b.CreateString(identifier);
      var motor_vector = FActor.CreateMotorsVector(b, motors);
      FActor.StartFActor(b);
      if (actor is KillableActor) {
        FActor.AddAlive(b, ((KillableActor)actor).IsAlive);
      } else {
        FActor.AddAlive(b, true);
      }

      FActor.AddActorName(b, n);
      FActor.AddMotors(b, motor_vector);
      return FActor.EndFActor(b);
    }

    static Offset<FOBS> build_observer(FlatBufferBuilder b, Observer observer) {
      var n = b.CreateString(observer.Identifier);

      Int32 observation_offset;
      FObservation observation_type;

      if (observer is IHasArray) {
        observation_offset = build_array(b, (IHasArray)observer).Value;
        observation_type = FObservation.FArray;
      } else if (observer is IHasSingle) {
        observation_offset = build_single(b, (IHasSingle)observer).Value;
        observation_type = FObservation.FSingle;
      } else if (observer is IHasDouble) {
        observation_offset = build_double(b, (IHasDouble)observer).Value;
        observation_type = FObservation.FDouble;
      } else if (observer is IHasTriple) {
        observation_offset = build_triple(b, (IHasTriple)observer).Value;
        observation_type = FObservation.FTriple;
      } else if (observer is IHasQuadruple) {
        observation_offset = build_quadruple(b, (IHasQuadruple)observer).Value;
        observation_type = FObservation.FQuadruple;
      } else if (observer is IHasEulerTransform) {
        observation_offset = build_euler_transform(b, (IHasEulerTransform)observer).Value;
        observation_type = FObservation.FET;
      } else if (observer is IHasQuaternionTransform) {
        observation_offset = build_quaternion_transform(b, (IHasQuaternionTransform)observer).Value;
        observation_type = FObservation.FQT;
      } else if (observer is IHasRigidbody) {
        observation_offset = build_body_observation(
            b,
            ((IHasRigidbody)observer).Velocity,
            ((IHasRigidbody)observer).AngularVelocity).Value;
        observation_type = FObservation.FRB;
      } else if (observer is IHasByteArray) {
        observation_offset = build_byte_array(b, (IHasByteArray)observer).Value;
        observation_type = FObservation.FByteArray;
      } else {
        return FOBS.CreateFOBS(b, n);
      }

      FOBS.StartFOBS(b);
      FOBS.AddObservationName(b, n);
      FOBS.AddObservationType(b, observation_type);
      FOBS.AddObservation(b, observation_offset);
      return FOBS.EndFOBS(b);
    }

    static Offset<FEnvironmentDescription> build_description(
        FlatBufferBuilder b,
        Messages.EnvironmentState state) {
      var actors = new Offset<FActor>[state.Description.Actors.Values.Count];
      var j = 0;
      foreach (var actor in state.Description.Actors) {
        var motors = new Offset<FMotor>[actor.Value.Motors.Values.Count];
        var i = 0;
        foreach (var motor in actor.Value.Motors) {
          motors[i++] = build_motor(b, motor.Value, motor.Key);
        }

        actors[j++] = build_actor(b, motors, actor.Value, actor.Key);
      }

      var actors_vector = FEnvironmentDescription.CreateActorsVector(b, actors);

      var configurables = new Offset<FConfigurable>[state.Description.Configurables.Values.Count];
      var k = 0;
      foreach (var configurable in state.Description.Configurables) {
        configurables[k++] = build_configurable(b, configurable.Value, configurable.Key);
      }

      var configurables_vector = FEnvironmentDescription.CreateConfigurablesVector(b, configurables);

      var api_version_offset = b.CreateString(state.Description.ApiVersion);

      var objective_offset = build_objective(b, state.Description);

      FEnvironmentDescription.StartFEnvironmentDescription(b);
      FEnvironmentDescription.AddObjective(b, objective_offset);

      FEnvironmentDescription.AddActors(b, actors_vector);
      FEnvironmentDescription.AddConfigurables(b, configurables_vector);
      FEnvironmentDescription.AddApiVersion(b, api_version_offset);
      return FEnvironmentDescription.EndFEnvironmentDescription(b);
    }

    static Offset<FObjective> build_objective(
        FlatBufferBuilder b,
        Messages.EnvironmentDescription description) {
      var objective_name_offset = b.CreateString("Default objective");
      FObjective.StartFObjective(b);
      FObjective.AddMaxEpisodeLength(b, description.MaxSteps);
      FObjective.AddSolvedThreshold(b, description.SolvedThreshold);
      FObjective.AddObjectiveName(b, objective_name_offset);
      return FObjective.EndFObjective(b);
    }

    static Offset<FTriple> build_position(FlatBufferBuilder b, PositionConfigurable observer) {
      var pos = observer.ObservationValue;
      FTriple.StartFTriple(b);
      FTriple.AddVec3(b, FVector3.CreateFVector3(b, pos.x, pos.y, pos.z));
      return FTriple.EndFTriple(b);
    }

    static Offset<FConfigurable> build_configurable(
        FlatBufferBuilder b,
        ConfigurableGameObject configurable,
        string identifier) {
      var n = b.CreateString(identifier);

      Int32 observation_offset;
      FObservation observation_type;

      if (configurable is IHasQuaternionTransform) {
        observation_offset = build_quaternion_transform(b, (IHasQuaternionTransform)configurable).Value;
        observation_type = FObservation.FQT;
      } else if (configurable is PositionConfigurable) {
        observation_offset = build_position(b, (PositionConfigurable)configurable).Value;
        observation_type = FObservation.FTriple;
      } else if (configurable is IHasSingle) {
        observation_offset = build_single(b, (IHasSingle)configurable).Value;
        observation_type = FObservation.FSingle;
      } else if (configurable is IHasDouble) {
        observation_offset = build_double(b, (IHasDouble)configurable).Value;
        observation_type = FObservation.FDouble;
      } else if (configurable is EulerTransformConfigurable) {
        observation_offset = build_euler_transform(b, (IHasEulerTransform)configurable).Value;
        observation_type = FObservation.FET;
      } else {
        FConfigurable.StartFConfigurable(b);
        FConfigurable.AddConfigurableName(b, n);
        return FConfigurable.EndFConfigurable(b);
      }

      FConfigurable.StartFConfigurable(b);
      FConfigurable.AddConfigurableName(b, n);
      FConfigurable.AddObservation(b, observation_offset);
      FConfigurable.AddObservationType(b, observation_type);
      return FConfigurable.EndFConfigurable(b);
    }

    #endregion
  }
}
