﻿namespace Example.ImGuizmoDemo
{
    using Example.ImGuiDemo;
    using HexaEngine.Core;
    using HexaEngine.Core.Graphics;
    using HexaEngine.Core.Input;
    using HexaEngine.Core.Windows.Events;
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGuizmo;
    using HexaEngine.Mathematics;
    using System;
    using System.Numerics;

    public class ImGuizmoDemo
    {
        private IGraphicsDevice device;

        public ImGuizmoDemo()
        {
        }

        private CameraTransform camera = new();
        private Vector3 sc = new(10, 0.5f, 0.5f);
        private const float speed = 2;
        private bool first = true;

        private string[] operationNames = Enum.GetNames<ImGuizmoOperation>();
        private ImGuizmoOperation[] operations = Enum.GetValues<ImGuizmoOperation>();

        private string[] modeNames = Enum.GetNames<ImGuizmoMode>();
        private ImGuizmoMode[] modes = Enum.GetValues<ImGuizmoMode>();

        private ImGuizmoOperation operation = ImGuizmoOperation.Universal;
        private ImGuizmoMode mode = ImGuizmoMode.Local;

        private Viewport SourceViewport = new(1920, 1080);
        private Viewport Viewport;
        private bool gimbalGrabbed;
        private Matrix4x4 cube = Matrix4x4.Identity;
        private bool overGimbal;

        public unsafe void Init(IGraphicsDevice device)
        {
            var sw = Application.MainWindow.SwapChain;
            this.device = device;
            Application.MainWindow.SwapChain.Resized += Resized;
            SourceViewport = new(sw.Width, sw.Height);
        }

        private void Resized(object? sender, ResizedEventArgs e)
        {
            SourceViewport = new(e.NewWidth, e.NewHeight);
        }

        public void Draw()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            if (!ImGui.Begin("Demo ImGuizmo", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoMove))
            {
                ImGui.PopStyleColor(1);
                ImGui.End();
                return;
            }
            ImGui.PopStyleColor(1);

            if (!ImGui.IsWindowDocked())
            {
                var node = ImGui.DockBuilderGetCentralNode(ImGuiManager.DockSpaceId);
                ImGui.DockBuilderDockWindow("Demo ImGuizmo", node.ID);
            }

            HandleInput();
            DrawMenuBar();

            var position = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();
            float ratioX = size.X / SourceViewport.Width;
            float ratioY = size.Y / SourceViewport.Height;
            var s = Math.Min(ratioX, ratioY);
            var w = SourceViewport.Width * s;
            var h = SourceViewport.Height * s;
            var x = position.X + (size.X - w) / 2;
            var y = position.Y + (size.Y - h) / 2;
            Viewport = new Viewport(x, y, w, h);

            var view = camera.View;
            var proj = camera.Projection;
            ImGuizmo.SetDrawlist();
            ImGuizmo.Enable(true);
            ImGuizmo.SetOrthographic(false);
            ImGuizmo.SetRect(position.X, position.Y, size.X, size.Y);

            var transform = cube;

            Matrix4x4 matrix = Matrix4x4.Identity;
            ImGuizmo.DrawGrid(ref view, ref proj, ref matrix, 10);
            ImGuizmo.DrawCubes(ref view, ref proj, ref transform, 1);

            ImGuizmo.SetID(0);

            if (ImGuizmo.Manipulate(ref view, ref proj, operation, mode, ref transform))
            {
                gimbalGrabbed = true;
                cube = transform;
            }

            if (!ImGuizmo.IsUsing() && gimbalGrabbed)
            {
                gimbalGrabbed = false;
            }
            overGimbal = ImGuizmo.IsOver();

            ImGui.PushItemWidth(100);
            int opIndex = Array.IndexOf(operations, operation);
            if (ImGui.Combo("##Operation", ref opIndex, operationNames, operationNames.Length))
            {
                operation = operations[opIndex];
            }
            int modeIndex = Array.IndexOf(modes, mode);
            if (ImGui.Combo("##Mode", ref modeIndex, modeNames, modeNames.Length))
            {
                mode = modes[modeIndex];
            }
            ImGui.PopItemWidth();

            ImGui.Text($"IsOver: {overGimbal}");
            ImGui.Text($"IsUsed: {gimbalGrabbed}");

            ImGui.End();
        }

        private void HandleInput()
        {
            if (ImGui.IsWindowHovered())
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Middle) || Keyboard.IsDown(Key.LCtrl) || first)
                {
                    Vector2 delta = Vector2.Zero;
                    if (Mouse.IsDown(MouseButton.Middle))
                    {
                        delta = Mouse.Delta;
                    }

                    float wheel = 0;
                    if (Keyboard.IsDown(Key.LCtrl))
                    {
                        wheel = Mouse.DeltaWheel.Y;
                    }

                    // Only update the camera's position if the mouse got moved in either direction
                    if (delta.X != 0f || delta.Y != 0f || wheel != 0f || first)
                    {
                        sc.X += sc.X / 2 * -wheel;

                        // Rotate the camera left and right
                        sc.Y += -delta.X * Time.Delta * speed;

                        // Rotate the camera up and down
                        // Prevent the camera from turning upside down (1.5f = approx. Pi / 2)
                        sc.Z = Math.Clamp(sc.Z + delta.Y * Time.Delta * speed, -MathF.PI / 2, MathF.PI / 2);

                        first = false;

                        // Calculate the cartesian coordinates
                        Vector3 pos = SphereHelper.GetCartesianCoordinates(sc);
                        var orientation = Quaternion.CreateFromYawPitchRoll(-sc.Y, sc.Z, 0);
                        camera.PositionRotation = (pos, orientation);
                        camera.Recalculate();
                    }
                }
            }
        }

        private void DrawMenuBar()
        {
            if (!ImGui.BeginMenuBar())
            {
                ImGui.EndMenuBar();
                return;
            }

            ImGui.EndMenuBar();
        }
    }
}