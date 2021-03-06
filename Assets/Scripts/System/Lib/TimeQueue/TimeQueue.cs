﻿
using CatLib.API.TimeQueue;
using System;
using System.Collections.Generic;

namespace CatLib.TimeQueue
{

    public class TimeQueue : Component , ITimeQueue
    {

        private Action queueOnComplete;
        private Action<object> queueOnCompleteWithContext;
        private object context;

        private List<TimeTask> queueTasks = new List<TimeTask>();
        public bool IsComplete { get; set; }

        public TimeRunner Runner{ get; set; }

        public ITimeTaskHandler Push(TimeTask task)
        {
            queueTasks.Add(task);
            return task;
        }

        public void Cancel(TimeTask task)
        {
            queueTasks.Remove(task);
        }

        public ITimeTask Task(Action task)
        {
            TimeTask timeTask = new TimeTask(this)
            {
                ActionTask = task
            };
            return timeTask;
        }

        public ITimeTask Task(Action<object> task)
        {
            TimeTask timeTask = new TimeTask(this)
            {
                ActionTaskWithContext = task
            };
            return timeTask;
        }

        public ITimeQueue OnComplete(Action<object> onComplete)
        {
            queueOnCompleteWithContext = onComplete;
            return this;
        }

        public ITimeQueue OnComplete(Action onComplete)
        {
            queueOnComplete = onComplete;
            return this;
        }

        public ITimeQueue SetContext(object context)
        {
            this.context = context;
            return this;
        }

        public bool Pause()
        {
            return Runner.StopRunner(this);
        }

        public bool Play()
        {
            IsComplete = false;
            return Runner.Runner(this);
        }

        public bool Stop()
        {
            bool statu = Runner.StopRunner(this);
            if (statu)
            {
                Reset();
            }
            return statu;
        }

        public bool Replay()
        {
            var statu = Stop();
            if (statu)
            {
                statu = Play();
            }
            return statu;
        }

        private void Reset()
        {
            TimeTask task;
            for (int i = 0; i < queueTasks.Count; ++i)
            {
                task = queueTasks[i];
                task.IsComplete = false;
                task.WaitDelayTime = 0;
                task.WaitLoopTime = 0;
            }
        }

        protected void CallTask(TimeTask task)
        {
            if (task.ActionTask != null)
            {
                task.ActionTask();
            }
            if (task.ActionTaskWithContext != null)
            {
                task.ActionTaskWithContext(context);
            }
        }

        public void Update()
        {
            bool isAllComplete = true;
            for (int i = 0; i < queueTasks.Count; ++i)
            {
                var task = queueTasks[i];
                if (task.IsComplete) { continue; }
                
                isAllComplete = false;

                if (task.DelayTime > 0 && task.WaitDelayTime < task.DelayTime)
                {
                    task.WaitDelayTime += App.Time.DeltaTime;
                    break;
                }

                if (task.loopStatusFunc == null)
                {
                    if (task.LoopTime > 0 && task.WaitLoopTime < task.LoopTime)
                    {
                        CallTask(task);
                        task.WaitLoopTime += App.Time.DeltaTime;
                        break;
                    }else if(task.LoopTime <= 0)
                    {
                        CallTask(task);
                    }
                }else
                {
                    if (task.loopStatusFunc.Invoke())
                    {
                        CallTask(task);
                        break;
                    }
                }
                task.IsComplete = true;
                if (task.TaskOnComplete != null)
                {
                    task.TaskOnComplete.Invoke();
                }
                if (task.TaskOnCompleteWithContext != null)
                {
                    task.TaskOnCompleteWithContext.Invoke(context);
                }
            }

            if (isAllComplete)
            {
                if(queueOnComplete != null)
                {
                    queueOnComplete.Invoke();
                }
                if(queueOnCompleteWithContext != null)
                {
                    queueOnCompleteWithContext.Invoke(context);
                }
                IsComplete = true;
            }
        }

    }

}