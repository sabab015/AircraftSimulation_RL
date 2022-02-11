using System;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {

        // movement variables
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostSpeed = 2;

        //explosions

        public GameObject meshObject;
        public GameObject explosionEffect;

        public int StepTimeout = 300;
        public int NextCheckPointIndex { get;  set; }

        // training area components 
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;
        private float nextStepTimeout;

        private bool frozen = false;
        //airplane controls

        private float pitchChange = 0f;
        private float smoothPitchChnage = 0f;
        private float MaxpitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;
        internal int NextCheckpointIndex;

        public override void Initialize()
        {
            
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
            //oveririding the max step. max step 5000 if in training mode, and infinite if racing. 
           MaxStep = area.trainingMode ? 5000 : 0;
        }

       public override void OnEpisodeBegin() // this function is called when a new training episode begins. 
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this, randomize: area.trainingMode);
           

            if (area.trainingMode) nextStepTimeout = StepCount + StepTimeout; // updating the step timeout in training mode. 
        }

        public override void OnActionReceived(float[] vectorAction)
        {
            if (frozen) return;

            pitchChange = vectorAction[0]; // the agent will go up, or stay in position. 
            if (pitchChange==2) 
            {
                pitchChange = -1f; // the agent will move downward. 
            }
            yawChange = vectorAction[1]; // the agent will turn right or stay in position.
            if (yawChange==2)
            {
                yawChange = -1f; // if the choice is 2, then the agent will turn left. 
            }

            boost = vectorAction[2] == 1;

            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            ProcessMovement(); 
            
            if (area.trainingMode)
            {
                AddReward(-1f / MaxStep);

                if(StepCount> nextStepTimeout)
                {
                    AddReward(-.5f);
                    EndEpisode();
                }
                Vector3 localCheckpointDir = VectorToNextCheckpoint();

                if(localCheckpointDir.magnitude< Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f))
                {
                    GoCheckPoint();
                }
            }
        }

       public override void CollectObservations(VectorSensor sensor) // collect observation from the world. 
        {
            sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity)); //observing aircraft velocity
            sensor.AddObservation(VectorToNextCheckpoint()); // observing the position of checkpoints. 
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckPointIndex].transform.forward; 
            sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward)); //observing the orientation of next checkpoints

            // The function will take Vector3 co-ordinates, which means each observation will receive 3 values each. 

        } 

        public override void Heuristic(float[] actionsOut)
        {
            Debug.LogError("Heuristic was called on "+ gameObject.name+ "Make sure the AircraftPlayer has a Heuristic type behavior only");
        }

       public void FreezeAgent() // freeze agent movement. 
        {
            Debug.Assert(area.trainingMode == false, "freeze/thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        } 

        public void ThawAgent() //resume agent movement.
        {
            Debug.Assert(area.trainingMode == false, "freeze/thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
            
        }

        private Vector3 VectorToNextCheckpoint() //this function gets a vector checkpoint and return that co-ordinate to the agent to fly through
        {
            Vector3 nextCheckpointdir = area.Checkpoints[NextCheckPointIndex].transform.position - transform.position;
            Vector3 localCheckpointdir = transform.InverseTransformDirection(nextCheckpointdir);
            return localCheckpointdir;
        }

        private void GoCheckPoint() // this function is called when the aircraft goes through the right checkpoint. 
        {
            NextCheckPointIndex = (NextCheckPointIndex + 1) % area.Checkpoints.Count;

            if(area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = StepCount + StepTimeout;

                
            }
        }

        internal void GiveModel(string v, Unity.Barracuda.NNModel model)
        {
            throw new NotImplementedException();
        }

        private void ProcessMovement()
        {
            float boostmodifier = boost ? boostSpeed : 1f;

            rigidbody.AddForce(transform.forward * thrust * boostmodifier, ForceMode.Force); // how fast the agent can move forward. 

            Vector3 currRotation = transform.rotation.eulerAngles;

            float rollAngle = currRotation.z > 180f ? currRotation.z - 360f : currRotation.z; // if the rolling angle is greater than 180, then this code will subtract the angle by 360. 

            if (yawChange== 0f)
            {
                rollChange = -rollAngle / maxRollAngle;

            }
            else
            {
                rollChange = -yawChange;
            }

            // this whole code deals with the rate of turning angle to smooth out the movement. 

            smoothPitchChnage = Mathf.MoveTowards(smoothPitchChnage, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            float pitch = currRotation.x + smoothPitchChnage * Time.fixedDeltaTime * pitchSpeed;

            if(pitch>180f)
            {
                pitch -= 360f; // keeping the pitch between -180f and 180f
            }

            pitch = Mathf.Clamp(pitch, -MaxpitchAngle, MaxpitchAngle);

            float yaw = currRotation.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            float roll = currRotation.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;

            if (roll > 180f)
            {
                roll -= 360f; // keeping the pitch between -180f and 180f
            }

            roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        private void OnTriggerEnter(Collider other) // react to entering triggr points 
        {
            if(other.transform.CompareTag("checkpoint") && other.gameObject== area.Checkpoints[NextCheckPointIndex])
            {
                GoCheckPoint();
            }
        }

        private void OnCollisionEnter(Collision collision) // react to collisions
        {
            if(!collision.transform.CompareTag("agent"))
            {
                if(area.trainingMode)
                {
                    AddReward(-1f);
                    EndEpisode();
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }
        private IEnumerator ExplosionReset() //resets the aircraft to most recent checkpoint if destroyed. 
        {
            FreezeAgent();

            meshObject.SetActive(false);    //disabling aircraft mesh, enabling explosion.
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            //enabling aircraft mesh, disabling explosion
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);

            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();

        }

    }

}

