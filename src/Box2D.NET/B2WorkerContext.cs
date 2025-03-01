// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2WorkerContext
    {
        public B2StepContext context;
        public int workerIndex;
        public object userTask;

        public void Clear()
        {
            context = null;
            workerIndex = -1;
            userTask = null;
        }
    }
}
