using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NunuMascotSaver;

internal sealed class MascotSimulation : IDisposable
{
    private readonly Random _random = new();
    private readonly List<MascotState> _mascots = [];
    private readonly Dictionary<long, List<int>> _grid = [];
    private readonly List<List<int>> _listPool = [];
    private readonly PoseSprite[] _poseSprites;

    private readonly int _size;
    private readonly bool _squishEnabled;
    private readonly bool _giantMascotEnabled;
    private readonly float _giantSpawnBaseSeconds;
    private readonly float _giantSpawnRandomSeconds;
    private readonly float _giantSizeScale;
    private readonly float _maxAspectRatio;
    private int _updateCount;

    private Rectangle _dirtyRect;
    private readonly List<MascotDrawCommand> _drawCommands = [];
    private readonly List<PointF> _textTargets = [];
    private int[] _textTargetByMascot = [];
    private PointF[] _formationOriginByMascot = [];
    private PointF[] _formationOriginVelocityByMascot = [];
    private PointF[] _transitionStartByMascot = [];
    private Pose[] _formationPoseByMascot = [];
    private FormationPhase _formationPhase = FormationPhase.None;
    private float _formationPhaseElapsedSeconds;
    private string _textFormationText = string.Empty;
    private Size _textFormationClientSize;
    private Rectangle[] _textPlacementAreas = [];

    private const float GatherDurationSeconds = 2.0f;
    private const float DisperseDurationSeconds = 2.0f;
    private const float FormationBobAmplitude = 1.8f;
    private const float FormationBobFrequencyScale = 0.72f;
    private const float SpeedTargetFactorMin = 0.78f;
    private const float SpeedTargetFactorMax = 1.34f;
    private const float GiantRiseSeconds = 1.15f;
    private const float GiantHoldSeconds = 2.30f;
    private const float GiantFallSeconds = 1.05f;

    private GiantMascotState _giantState = GiantMascotState.Hidden;
    private float _giantPhaseSeconds;
    private float _giantNextSpawnSeconds;
    private float _giantX;
    private float _giantY;
    private float _giantStartY;
    private float _giantPeakY;
    private float _giantWidth;
    private float _giantHeight;
    private int _giantPoseIndex;
    private Rectangle _giantBounds = Rectangle.Empty;

    public MascotSimulation(ScreenSaverSettings settings, Size clientSize)
    {
        _size = settings.MascotSize;
        _squishEnabled = settings.EnableSquish;
        _giantMascotEnabled = settings.EnableGiantMascotPopUp;
        _giantSpawnBaseSeconds = (float)Math.Max(1.0, settings.GiantMascotIntervalSeconds);
        _giantSpawnRandomSeconds = (float)Math.Max(0.0, settings.GiantMascotRandomIntervalSeconds);
        _giantSizeScale = Math.Max(1.0f, settings.GiantMascotSizePercent / 100f);
        _poseSprites = LoadPoseSprites(_size);
        _maxAspectRatio = _poseSprites.Max(sprite => sprite.AspectRatio);
        _giantNextSpawnSeconds = ResolveNextGiantSpawnSeconds();

        SyncCount(settings.MascotCount, clientSize);
    }

    public Rectangle DirtyRect => _dirtyRect;

    public void SetTextPlacementAreas(IReadOnlyList<Rectangle> areas, Size clientSize)
    {
        if (areas is null || areas.Count == 0)
        {
            _textPlacementAreas = [];
        }
        else
        {
            var normalized = new List<Rectangle>(areas.Count);
            for (int i = 0; i < areas.Count; i += 1)
            {
                Rectangle area = areas[i];
                if (area.Width <= 0 || area.Height <= 0)
                {
                    continue;
                }

                normalized.Add(area);
            }

            _textPlacementAreas = normalized.Count == 0 ? [] : [.. normalized];
        }

        if (IsFormationTextActive && _textTargets.Count > 0)
        {
            BuildTextTargets(_textFormationText, clientSize);
            StartGathering(captureOrigins: false);
        }
    }

    public void SetTextFormation(string? text, bool active, Size clientSize)
    {
        string normalizedText = (text ?? string.Empty).Trim();
        bool shouldActivate = active && normalizedText.Length > 0 && _mascots.Count > 0;
        if (!shouldActivate)
        {
            if (IsFormationTextActive)
            {
                StartDispersing();
            }

            return;
        }

        bool startingNewCycle = _formationPhase is FormationPhase.None or FormationPhase.Dispersing;
        bool needsRebuild =
            startingNewCycle ||
            _textFormationText != normalizedText ||
            _textFormationClientSize != clientSize ||
            _textTargetByMascot.Length != _mascots.Count ||
            _textTargets.Count == 0;

        _textFormationText = normalizedText;
        _textFormationClientSize = clientSize;

        if (needsRebuild)
        {
            BuildTextTargets(normalizedText, clientSize);
        }

        if (_textTargets.Count == 0 || _textTargetByMascot.Length != _mascots.Count)
        {
            ResetFormationPhase();
            return;
        }

        StartGathering(captureOrigins: startingNewCycle);
    }

    public ReadOnlySpan<MascotDrawCommand> BuildDrawCommands()
    {
        _drawCommands.Clear();

        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            Pose drawPose = mascot.Pose;
            if (IsFormationTextActive && (uint)i < (uint)_formationPoseByMascot.Length)
            {
                drawPose = _formationPoseByMascot[i];
            }

            PoseSprite sprite = _poseSprites[(int)drawPose];
            float bobOffset =
                _formationPhase == FormationPhase.None
                    ? MathF.Sin(mascot.BobPhase) * 3f
                    : (IsFormationTextActive
                        ? MathF.Sin(mascot.BobPhase * FormationBobFrequencyScale) * FormationBobAmplitude
                        : 0f);

            float baseWidth = _size;
            float baseHeight = baseWidth * sprite.AspectRatio;

            if (_squishEnabled && (mascot.SquishX != 1f || mascot.SquishY != 1f))
            {
                float drawWidth = baseWidth * mascot.SquishX;
                float drawHeight = baseHeight * mascot.SquishY;
                float drawX = mascot.X + (baseWidth - drawWidth) * 0.5f;
                float drawY = mascot.Y + bobOffset + (baseHeight - drawHeight) * 0.5f;

                _drawCommands.Add(new MascotDrawCommand((int)drawPose, drawX, drawY, drawWidth, drawHeight, true));
            }
            else
            {
                float drawX = mascot.X;
                float drawY = mascot.Y + bobOffset;
                _drawCommands.Add(new MascotDrawCommand((int)drawPose, drawX, drawY, sprite.ScaledWidth, sprite.ScaledHeight, false));
            }
        }

        if (_giantState != GiantMascotState.Hidden && (uint)_giantPoseIndex < (uint)_poseSprites.Length)
        {
            _drawCommands.Add(new MascotDrawCommand(_giantPoseIndex, _giantX, _giantY, _giantWidth, _giantHeight, true));
        }

        return CollectionsMarshal.AsSpan(_drawCommands);
    }

    public void Update(float dt, Size clientSize)
    {
        if (_mascots.Count == 0)
        {
            return;
        }

        _dirtyRect = Rectangle.Empty;

        (float maxX, float maxY) = GetMovementBounds(clientSize);
        float spriteHeight = _size * _maxAspectRatio;
        UpdateGiantMascot(dt, clientSize);

        if (_formationPhase != FormationPhase.None)
        {
            UpdateFormationPhase(dt, maxX, maxY, spriteHeight);
            ResolveGiantCollisions(maxX, maxY);
            return;
        }

        foreach (MascotState mascot in _mascots)
        {
            ExpandDirty(mascot, spriteHeight);

            mascot.X += mascot.Vx * dt;
            mascot.Y += mascot.Vy * dt;

            if (mascot.X <= 0 || mascot.X >= maxX)
            {
                if (_squishEnabled)
                {
                    ApplyWallSquish(mascot, isX: true, ImpactFromSpeed(mascot.Vx, 85f));
                }

                mascot.Vx *= -1f;
                mascot.X = Math.Clamp(mascot.X, 0, maxX);
            }

            if (mascot.Y <= 0 || mascot.Y >= maxY)
            {
                if (_squishEnabled)
                {
                    ApplyWallSquish(mascot, isX: false, ImpactFromSpeed(mascot.Vy, 85f));
                }

                mascot.Vy *= -1f;
                mascot.Y = Math.Clamp(mascot.Y, 0, maxY);
            }
        }

        _updateCount += 1;
        if (ShouldResolveCollisions(clientSize))
        {
            int maxChecksPerMascot = ResolveMaxChecksPerMascot(clientSize);
            ResolveCollisions(maxX, maxY, maxChecksPerMascot);
        }

        ResolveGiantCollisions(maxX, maxY);

        foreach (MascotState mascot in _mascots)
        {
            ApplyRandomizedCruiseSpeed(mascot, dt);
            mascot.BobPhase += dt * 4f;

            if (_squishEnabled)
            {
                if (mascot.SquishTimer > 0)
                {
                    mascot.SquishTimer -= dt;
                }
                else
                {
                    float ease = Math.Min(1f, dt * 12f);
                    mascot.SquishX += (1f - mascot.SquishX) * ease;
                    mascot.SquishY += (1f - mascot.SquishY) * ease;
                }
            }

            mascot.Pose = ResolvePose(mascot.Vx, mascot.Vy);

            ExpandDirty(mascot, spriteHeight);
        }
    }

    public void Draw(Graphics graphics)
    {
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.PixelOffsetMode = PixelOffsetMode.None;
        InterpolationMode currentInterpolation = InterpolationMode.NearestNeighbor;
        graphics.InterpolationMode = currentInterpolation;

        ReadOnlySpan<MascotDrawCommand> commands = BuildDrawCommands();
        for (int i = 0; i < commands.Length; i += 1)
        {
            ref readonly MascotDrawCommand command = ref commands[i];
            PoseSprite sprite = _poseSprites[command.PoseIndex];
            if (command.UseLinearSampling)
            {
                if (currentInterpolation != InterpolationMode.Bilinear)
                {
                    currentInterpolation = InterpolationMode.Bilinear;
                    graphics.InterpolationMode = currentInterpolation;
                }
                graphics.DrawImage(sprite.Image, command.X, command.Y, command.Width, command.Height);
            }
            else
            {
                if (currentInterpolation != InterpolationMode.NearestNeighbor)
                {
                    currentInterpolation = InterpolationMode.NearestNeighbor;
                    graphics.InterpolationMode = currentInterpolation;
                }
                graphics.DrawImage(sprite.Scaled, command.X, command.Y, command.Width, command.Height);
            }
        }
    }

    public void SyncCount(int desiredCount, Size clientSize)
    {
        int safeCount = Math.Max(0, desiredCount);
        (float maxX, float maxY) = GetMovementBounds(clientSize);

        while (_mascots.Count < safeCount)
        {
            _mascots.Add(CreateMascot(maxX, maxY));
        }

        while (_mascots.Count > safeCount)
        {
            _mascots.RemoveAt(_mascots.Count - 1);
        }

        if (IsFormationTextActive)
        {
            BuildTextTargets(_textFormationText, clientSize);
            StartGathering(captureOrigins: true);
        }
    }

    public void ClampToBounds(Size clientSize)
    {
        (float maxX, float maxY) = GetMovementBounds(clientSize);

        foreach (MascotState mascot in _mascots)
        {
            mascot.X = Math.Clamp(mascot.X, 0, maxX);
            mascot.Y = Math.Clamp(mascot.Y, 0, maxY);
        }
    }

    public void Dispose()
    {
        foreach (PoseSprite sprite in _poseSprites)
        {
            sprite.Dispose();
        }

        _mascots.Clear();
        _textTargets.Clear();
        _textTargetByMascot = [];
        _formationOriginByMascot = [];
        _formationOriginVelocityByMascot = [];
        _transitionStartByMascot = [];
        _formationPoseByMascot = [];
        _formationPhase = FormationPhase.None;
        _giantState = GiantMascotState.Hidden;
        _giantBounds = Rectangle.Empty;
        _textPlacementAreas = [];
        ReturnAllLists();
        _grid.Clear();
        _listPool.Clear();
    }

    private void ExpandDirty(MascotState mascot, float spriteHeight)
    {
        const int pad = 6;
        int x = (int)mascot.X - pad;
        int y = (int)(mascot.Y - 3f) - pad;
        int w = _size + pad * 2;
        int h = (int)MathF.Ceiling(spriteHeight) + pad * 2 + 6;
        var rect = new Rectangle(x, y, w, h);

        ExpandDirtyRect(rect);
    }

    private void ExpandDirtyRect(Rectangle rect)
    {
        if (rect.IsEmpty)
        {
            return;
        }

        _dirtyRect = _dirtyRect.IsEmpty ? rect : Rectangle.Union(_dirtyRect, rect);
    }

    private void UpdateGiantMascot(float dt, Size clientSize)
    {
        Rectangle previousBounds = _giantBounds;

        if (!_giantMascotEnabled)
        {
            _giantState = GiantMascotState.Hidden;
            _giantPhaseSeconds = 0f;
            _giantBounds = Rectangle.Empty;
            ExpandDirtyRect(previousBounds);
            return;
        }

        int clientWidth = Math.Max(1, clientSize.Width);
        int clientHeight = Math.Max(1, clientSize.Height);

        switch (_giantState)
        {
            case GiantMascotState.Hidden:
                _giantNextSpawnSeconds -= dt;
                if (_giantNextSpawnSeconds <= 0f)
                {
                    BeginGiantMascot(clientWidth, clientHeight);
                }

                break;

            case GiantMascotState.Rising:
                _giantPhaseSeconds = Math.Min(GiantRiseSeconds, _giantPhaseSeconds + dt);
                float riseProgress = _giantPhaseSeconds / GiantRiseSeconds;
                _giantY = Lerp(_giantStartY, _giantPeakY, EaseOutCubic(riseProgress));
                if (riseProgress >= 1f)
                {
                    _giantState = GiantMascotState.Holding;
                    _giantPhaseSeconds = 0f;
                }

                break;

            case GiantMascotState.Holding:
                _giantPhaseSeconds += dt;
                _giantY = _giantPeakY + MathF.Sin(_giantPhaseSeconds * 2.4f) * 2.0f;
                if (_giantPhaseSeconds >= GiantHoldSeconds)
                {
                    _giantState = GiantMascotState.Falling;
                    _giantPhaseSeconds = 0f;
                }

                break;

            case GiantMascotState.Falling:
                _giantPhaseSeconds = Math.Min(GiantFallSeconds, _giantPhaseSeconds + dt);
                float fallProgress = _giantPhaseSeconds / GiantFallSeconds;
                _giantY = Lerp(_giantPeakY, _giantStartY, EaseInCubic(fallProgress));
                if (fallProgress >= 1f)
                {
                    _giantState = GiantMascotState.Hidden;
                    _giantPhaseSeconds = 0f;
                    _giantNextSpawnSeconds = ResolveNextGiantSpawnSeconds();
                    _giantBounds = Rectangle.Empty;
                    ExpandDirtyRect(previousBounds);
                    return;
                }

                break;
        }

        if (_giantState == GiantMascotState.Hidden)
        {
            _giantBounds = Rectangle.Empty;
        }
        else
        {
            Rectangle currentBounds = Rectangle.Ceiling(new RectangleF(_giantX, _giantY, _giantWidth, _giantHeight));
            currentBounds.Inflate(6, 6);
            _giantBounds = currentBounds;
        }

        ExpandDirtyRect(previousBounds);
        ExpandDirtyRect(_giantBounds);
    }

    private void BeginGiantMascot(int clientWidth, int clientHeight)
    {
        _giantPoseIndex = _random.Next(_poseSprites.Length);
        PoseSprite sprite = _poseSprites[_giantPoseIndex];

        float desiredWidth = _size * _giantSizeScale;
        float maxWidth = Math.Max(90f, clientWidth * 0.78f);
        _giantWidth = Math.Min(desiredWidth, maxWidth);

        _giantHeight = _giantWidth * sprite.AspectRatio;
        float maxHeight = Math.Max(100f, clientHeight * 0.96f);
        if (_giantHeight > maxHeight)
        {
            float scale = maxHeight / _giantHeight;
            _giantHeight = maxHeight;
            _giantWidth *= scale;
        }

        float maxX = Math.Max(0f, clientWidth - _giantWidth);
        _giantX = (float)_random.NextDouble() * maxX;
        _giantStartY = clientHeight + Math.Max(12f, _giantHeight * 0.14f);

        float visibleRatio = 0.72f + (float)_random.NextDouble() * 0.12f;
        float desiredPeakY = clientHeight - (_giantHeight * visibleRatio);
        float minPeakY = -_giantHeight * 0.24f;
        float maxPeakY = clientHeight - (_giantHeight * 0.34f);
        _giantPeakY = Math.Clamp(desiredPeakY, minPeakY, maxPeakY);

        _giantY = _giantStartY;
        _giantPhaseSeconds = 0f;
        _giantState = GiantMascotState.Rising;
    }

    private float ResolveNextGiantSpawnSeconds()
    {
        if (_giantSpawnRandomSeconds <= 0f)
        {
            return _giantSpawnBaseSeconds;
        }

        return _giantSpawnBaseSeconds + ((float)_random.NextDouble() * _giantSpawnRandomSeconds);
    }

    private static float Lerp(float from, float to, float t)
    {
        return from + ((to - from) * t);
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private (float MaxX, float MaxY) GetMovementBounds(Size clientSize)
    {
        float maxX = Math.Max(0, clientSize.Width - _size);
        float maxY = Math.Max(0, clientSize.Height - (_size * _maxAspectRatio));
        return (maxX, maxY);
    }

    private MascotState CreateMascot(float maxX, float maxY)
    {
        float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
        float speed = 30f + (float)_random.NextDouble() * 50f;
        float speedFactor = SpeedTargetFactorMin + (float)_random.NextDouble() * (SpeedTargetFactorMax - SpeedTargetFactorMin);

        return new MascotState
        {
            X = (float)_random.NextDouble() * maxX,
            Y = (float)_random.NextDouble() * maxY,
            Vx = MathF.Cos(angle) * speed,
            Vy = MathF.Sin(angle) * speed,
            BobPhase = (float)(_random.NextDouble() * Math.PI * 2.0),
            BaseSpeed = speed,
            SpeedTargetFactor = speedFactor,
            SpeedCurrentFactor = speedFactor,
            SpeedRetargetTimer = 0.6f + (float)_random.NextDouble() * 1.8f
        };
    }

    private void ResolveCollisions(float maxX, float maxY, int maxChecksPerMascot)
    {
        float cellSize = Math.Max(16f, _size * 0.95f);

        BuildSpatialGrid(cellSize);

        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState a = _mascots[i];
            PoseSprite spriteA = _poseSprites[(int)a.Pose];
            int cellX = (int)MathF.Floor(a.X / cellSize);
            int cellY = (int)MathF.Floor(a.Y / cellSize);
            int checks = 0;
            bool reachedBudget = false;

            for (int offsetX = -1; offsetX <= 1; offsetX += 1)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY += 1)
                {
                    long key = MakeCellKey(cellX + offsetX, cellY + offsetY);
                    if (!_grid.TryGetValue(key, out List<int>? indices))
                    {
                        continue;
                    }

                    for (int index = 0; index < indices.Count; index += 1)
                    {
                        int j = indices[index];
                        if (j <= i)
                        {
                            continue;
                        }

                        checks += 1;
                        if (checks > maxChecksPerMascot)
                        {
                            reachedBudget = true;
                            break;
                        }

                        MascotState b = _mascots[j];
                        PoseSprite spriteB = _poseSprites[(int)b.Pose];
                        float aCenterX = a.X + spriteA.CollisionCenterX;
                        float aCenterY = a.Y + spriteA.CollisionCenterY;
                        float bCenterX = b.X + spriteB.CollisionCenterX;
                        float bCenterY = b.Y + spriteB.CollisionCenterY;
                        float dx = bCenterX - aCenterX;
                        float dy = bCenterY - aCenterY;
                        float distSquared = dx * dx + dy * dy;
                        float broadMinDist = spriteA.CollisionMaxRadius + spriteB.CollisionMaxRadius;
                        float broadMinDistSquared = broadMinDist * broadMinDist;

                        if (distSquared >= broadMinDistSquared)
                        {
                            continue;
                        }

                        if (distSquared < 0.0001f)
                        {
                            dx = 1f;
                            dy = 0f;
                            distSquared = 1f;
                        }

                        float dist = MathF.Sqrt(distSquared);
                        float nx = dx / dist;
                        float ny = dy / dist;
                        float minDist = spriteA.GetDirectionalRadius(nx, ny) + spriteB.GetDirectionalRadius(-nx, -ny);
                        if (dist >= minDist)
                        {
                            continue;
                        }

                        float overlap = minDist - dist;

                        a.X -= nx * overlap * 0.5f;
                        a.Y -= ny * overlap * 0.5f;
                        b.X += nx * overlap * 0.5f;
                        b.Y += ny * overlap * 0.5f;

                        float relVx = b.Vx - a.Vx;
                        float relVy = b.Vy - a.Vy;
                        float closing = relVx * nx + relVy * ny;

                        if (closing < 0)
                        {
                            if (_squishEnabled)
                            {
                                float impact = ImpactFromSpeed(closing, 80f);
                                float compress = 0.22f * impact;
                                float expand = 0.16f * impact;
                                float nxAbs = Math.Abs(nx);
                                float nyAbs = Math.Abs(ny);
                                float scaleX = 1f - compress * nxAbs + expand * nyAbs;
                                float scaleY = 1f - compress * nyAbs + expand * nxAbs;

                                a.SquishX = scaleX;
                                a.SquishY = scaleY;
                                b.SquishX = scaleX;
                                b.SquishY = scaleY;
                                a.SquishTimer = 0.12f;
                                b.SquishTimer = 0.12f;
                            }

                            float va = a.Vx * nx + a.Vy * ny;
                            float vb = b.Vx * nx + b.Vy * ny;
                            float impulse = vb - va;

                            a.Vx += impulse * nx;
                            a.Vy += impulse * ny;
                            b.Vx -= impulse * nx;
                            b.Vy -= impulse * ny;
                        }
                    }

                    if (reachedBudget)
                    {
                        break;
                    }
                }

                if (reachedBudget)
                {
                    break;
                }
            }
        }

        foreach (MascotState mascot in _mascots)
        {
            mascot.X = Math.Clamp(mascot.X, 0, maxX);
            mascot.Y = Math.Clamp(mascot.Y, 0, maxY);
        }
    }

    private void ResolveGiantCollisions(float maxX, float maxY)
    {
        if (_giantState == GiantMascotState.Hidden || _mascots.Count == 0)
        {
            return;
        }

        if ((uint)_giantPoseIndex >= (uint)_poseSprites.Length || _giantWidth <= 1f || _giantHeight <= 1f)
        {
            return;
        }

        PoseSprite giantSprite = _poseSprites[_giantPoseIndex];
        float giantScaleX = _giantWidth / Math.Max(1f, giantSprite.ScaledWidth);
        float giantScaleY = _giantHeight / Math.Max(1f, giantSprite.ScaledHeight);

        float giantCenterX = _giantX + (giantSprite.CollisionCenterX * giantScaleX);
        float giantCenterY = _giantY + (giantSprite.CollisionCenterY * giantScaleY);
        float giantRadiusX = Math.Max(1.0f, giantSprite.CollisionRadiusX * giantScaleX);
        float giantRadiusY = Math.Max(1.0f, giantSprite.CollisionRadiusY * giantScaleY);
        float giantMaxRadius = Math.Max(giantRadiusX, giantRadiusY);

        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            PoseSprite sprite = _poseSprites[(int)mascot.Pose];

            float mascotCenterX = mascot.X + sprite.CollisionCenterX;
            float mascotCenterY = mascot.Y + sprite.CollisionCenterY;
            float dx = giantCenterX - mascotCenterX;
            float dy = giantCenterY - mascotCenterY;
            float distSquared = (dx * dx) + (dy * dy);

            float broadMinDist = sprite.CollisionMaxRadius + giantMaxRadius;
            if (distSquared >= broadMinDist * broadMinDist)
            {
                continue;
            }

            if (distSquared < 0.0001f)
            {
                dx = 1f;
                dy = 0f;
                distSquared = 1f;
            }

            float dist = MathF.Sqrt(distSquared);
            float nx = dx / dist;
            float ny = dy / dist;
            float mascotRadius = sprite.GetDirectionalRadius(nx, ny);
            float giantRadius = GetDirectionalRadius(giantRadiusX, giantRadiusY, -nx, -ny);
            float minDist = mascotRadius + giantRadius;

            if (dist >= minDist)
            {
                continue;
            }

            float overlap = minDist - dist;
            mascot.X -= nx * overlap;
            mascot.Y -= ny * overlap;

            float gx = -nx;
            float gy = -ny;
            float closing = (mascot.Vx * gx) + (mascot.Vy * gy);
            if (closing < 0f)
            {
                const float restitution = 0.78f;
                mascot.Vx -= (1f + restitution) * closing * gx;
                mascot.Vy -= (1f + restitution) * closing * gy;

                if (_squishEnabled)
                {
                    float impact = ImpactFromSpeed(closing, 95f);
                    float compress = 0.20f * impact;
                    float expand = 0.14f * impact;
                    float gxAbs = Math.Abs(gx);
                    float gyAbs = Math.Abs(gy);
                    mascot.SquishX = 1f - (compress * gxAbs) + (expand * gyAbs);
                    mascot.SquishY = 1f - (compress * gyAbs) + (expand * gxAbs);
                    mascot.SquishTimer = 0.12f;
                }
            }

            mascot.X = Math.Clamp(mascot.X, 0f, maxX);
            mascot.Y = Math.Clamp(mascot.Y, 0f, maxY);
        }
    }

    private bool IsFormationTextActive =>
        _formationPhase is FormationPhase.Gathering or FormationPhase.Holding;

    private void UpdateFormationPhase(float dt, float maxX, float maxY, float spriteHeight)
    {
        switch (_formationPhase)
        {
            case FormationPhase.Gathering:
                UpdateFormationTransition(dt, maxX, maxY, spriteHeight, GatherDurationSeconds, useTextTargets: true);
                break;
            case FormationPhase.Holding:
                HoldFormationAtText(dt, maxX, maxY, spriteHeight);
                break;
            case FormationPhase.Dispersing:
                UpdateFormationTransition(dt, maxX, maxY, spriteHeight, DisperseDurationSeconds, useTextTargets: false);
                break;
        }
    }

    private void StartGathering(bool captureOrigins)
    {
        if (_mascots.Count == 0 || _textTargets.Count == 0 || _textTargetByMascot.Length != _mascots.Count)
        {
            ResetFormationPhase();
            return;
        }

        if (captureOrigins)
        {
            EnsureFormationArraySizes(_mascots.Count);
            for (int i = 0; i < _mascots.Count; i += 1)
            {
                MascotState mascot = _mascots[i];
                _formationOriginByMascot[i] = new PointF(mascot.X, mascot.Y);
                _formationOriginVelocityByMascot[i] = new PointF(mascot.Vx, mascot.Vy);
            }
        }

        EnsureFormationArraySizes(_mascots.Count);
        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            _transitionStartByMascot[i] = new PointF(mascot.X, mascot.Y);
        }

        AssignFormationPoses();
        _formationPhase = FormationPhase.Gathering;
        _formationPhaseElapsedSeconds = 0f;
    }

    private void StartDispersing()
    {
        if (_mascots.Count == 0 || _formationOriginByMascot.Length != _mascots.Count)
        {
            ResetFormationPhase();
            return;
        }

        EnsureFormationArraySizes(_mascots.Count);
        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            _transitionStartByMascot[i] = new PointF(mascot.X, mascot.Y);
        }

        _formationPhase = FormationPhase.Dispersing;
        _formationPhaseElapsedSeconds = 0f;
    }

    private void UpdateFormationTransition(
        float dt,
        float maxX,
        float maxY,
        float spriteHeight,
        float durationSeconds,
        bool useTextTargets)
    {
        float duration = Math.Max(0.01f, durationSeconds);
        _formationPhaseElapsedSeconds = Math.Min(duration, _formationPhaseElapsedSeconds + dt);
        float progress = _formationPhaseElapsedSeconds / duration;
        float eased = 1f - MathF.Pow(1f - progress, 3f);

        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            ExpandDirty(mascot, spriteHeight);

            PointF start =
                i < _transitionStartByMascot.Length
                    ? _transitionStartByMascot[i]
                    : new PointF(mascot.X, mascot.Y);
            PointF target = ResolveFormationTarget(i, useTextTargets, mascot.X, mascot.Y, maxX, maxY);

            float nextX = start.X + ((target.X - start.X) * eased);
            float nextY = start.Y + ((target.Y - start.Y) * eased);

            nextX = Math.Clamp(nextX, 0f, maxX);
            nextY = Math.Clamp(nextY, 0f, maxY);

            if (dt > 0f)
            {
                mascot.Vx = (nextX - mascot.X) / dt;
                mascot.Vy = (nextY - mascot.Y) / dt;
            }
            else
            {
                mascot.Vx = 0f;
                mascot.Vy = 0f;
            }

            mascot.X = nextX;
            mascot.Y = nextY;
            mascot.BobPhase += dt * 4f;
            mascot.Pose = ResolvePose(mascot.Vx, mascot.Vy);
            SettleSquish(mascot, dt);

            ExpandDirty(mascot, spriteHeight);
        }

        if (progress < 1f)
        {
            return;
        }

        if (useTextTargets)
        {
            _formationPhase = FormationPhase.Holding;
            _formationPhaseElapsedSeconds = 0f;
            return;
        }

        FinishDispersing(maxX, maxY, spriteHeight);
    }

    private void HoldFormationAtText(float dt, float maxX, float maxY, float spriteHeight)
    {
        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            ExpandDirty(mascot, spriteHeight);

            PointF target = ResolveFormationTarget(i, useTextTargets: true, mascot.X, mascot.Y, maxX, maxY);
            mascot.X = target.X;
            mascot.Y = target.Y;
            mascot.Vx = 0f;
            mascot.Vy = 0f;
            mascot.BobPhase += dt * 4f;
            mascot.Pose = Pose.Idle;
            SettleSquish(mascot, dt);

            ExpandDirty(mascot, spriteHeight);
        }
    }

    private PointF ResolveFormationTarget(
        int mascotIndex,
        bool useTextTargets,
        float fallbackX,
        float fallbackY,
        float maxX,
        float maxY)
    {
        if (useTextTargets)
        {
            int targetIndex =
                (uint)mascotIndex < (uint)_textTargetByMascot.Length
                    ? _textTargetByMascot[mascotIndex]
                    : -1;
            if ((uint)targetIndex < (uint)_textTargets.Count)
            {
                PointF target = _textTargets[targetIndex];
                return new PointF(
                    Math.Clamp(target.X, 0f, maxX),
                    Math.Clamp(target.Y, 0f, maxY));
            }
        }
        else if ((uint)mascotIndex < (uint)_formationOriginByMascot.Length)
        {
            PointF target = _formationOriginByMascot[mascotIndex];
            return new PointF(
                Math.Clamp(target.X, 0f, maxX),
                Math.Clamp(target.Y, 0f, maxY));
        }

        return new PointF(
            Math.Clamp(fallbackX, 0f, maxX),
            Math.Clamp(fallbackY, 0f, maxY));
    }

    private void FinishDispersing(float maxX, float maxY, float spriteHeight)
    {
        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            ExpandDirty(mascot, spriteHeight);

            if ((uint)i < (uint)_formationOriginByMascot.Length)
            {
                PointF origin = _formationOriginByMascot[i];
                mascot.X = Math.Clamp(origin.X, 0f, maxX);
                mascot.Y = Math.Clamp(origin.Y, 0f, maxY);
            }

            if ((uint)i < (uint)_formationOriginVelocityByMascot.Length)
            {
                PointF velocity = _formationOriginVelocityByMascot[i];
                mascot.Vx = velocity.X;
                mascot.Vy = velocity.Y;
            }
            else
            {
                mascot.Vx = 0f;
                mascot.Vy = 0f;
            }

            mascot.Pose = ResolvePose(mascot.Vx, mascot.Vy);
            SettleSquish(mascot, dt: 0.016f);
            ExpandDirty(mascot, spriteHeight);
        }

        _formationPhase = FormationPhase.None;
        _formationPhaseElapsedSeconds = 0f;
        _textTargets.Clear();
        _textTargetByMascot = [];
        _formationOriginByMascot = [];
        _formationOriginVelocityByMascot = [];
        _transitionStartByMascot = [];
        _formationPoseByMascot = [];
    }

    private void SettleSquish(MascotState mascot, float dt)
    {
        if (!_squishEnabled)
        {
            return;
        }

        mascot.SquishTimer = 0f;
        float settleBlend = Math.Min(1f, dt * 14f);
        mascot.SquishX += (1f - mascot.SquishX) * settleBlend;
        mascot.SquishY += (1f - mascot.SquishY) * settleBlend;
    }

    private void EnsureFormationArraySizes(int mascotCount)
    {
        if (_formationOriginByMascot.Length != mascotCount)
        {
            _formationOriginByMascot = new PointF[mascotCount];
        }

        if (_formationOriginVelocityByMascot.Length != mascotCount)
        {
            _formationOriginVelocityByMascot = new PointF[mascotCount];
        }

        if (_transitionStartByMascot.Length != mascotCount)
        {
            _transitionStartByMascot = new PointF[mascotCount];
        }

        if (_formationPoseByMascot.Length != mascotCount)
        {
            _formationPoseByMascot = new Pose[mascotCount];
        }
    }

    private void ResetFormationPhase()
    {
        _formationPhase = FormationPhase.None;
        _formationPhaseElapsedSeconds = 0f;
        _textTargets.Clear();
        _textTargetByMascot = [];
        _formationOriginByMascot = [];
        _formationOriginVelocityByMascot = [];
        _transitionStartByMascot = [];
        _formationPoseByMascot = [];
    }

    private void AssignFormationPoses()
    {
        if (_mascots.Count == 0)
        {
            return;
        }

        EnsureFormationArraySizes(_mascots.Count);
        for (int i = 0; i < _mascots.Count; i += 1)
        {
            _formationPoseByMascot[i] = _random.Next(4) switch
            {
                0 => Pose.Up,
                1 => Pose.Down,
                2 => Pose.Left,
                _ => Pose.Right
            };
        }
    }

    private void BuildTextTargets(string text, Size clientSize)
    {
        _textTargets.Clear();
        _textTargetByMascot = new int[_mascots.Count];
        Array.Fill(_textTargetByMascot, -1);
        if (_mascots.Count == 0)
        {
            return;
        }

        Rectangle placementArea = ResolveTextPlacementArea(clientSize);
        int width = Math.Max(1, placementArea.Width);
        int height = Math.Max(1, placementArea.Height);
        using var canvas = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using (Graphics graphics = Graphics.FromImage(canvas))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float fontSize = Math.Clamp(height * 0.28f, 24f, 300f);
            using var font = new Font("Yu Gothic UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisWord,
                FormatFlags = StringFormatFlags.NoWrap
            };
            RectangleF layout = new(8f, 8f, Math.Max(1, width - 16), Math.Max(1, height - 16));
            graphics.DrawString(text, font, Brushes.White, layout, format);
        }

        Rectangle rect = new(0, 0, canvas.Width, canvas.Height);
        BitmapData bitmapData = canvas.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        try
        {
            int stride = bitmapData.Stride;
            int byteCount = stride * bitmapData.Height;
            byte[] pixels = GC.AllocateUninitializedArray<byte>(byteCount);
            Marshal.Copy(bitmapData.Scan0, pixels, 0, byteCount);

            int step = Math.Clamp(_size / 3, 6, 20);
            float halfWidth = _size * 0.5f;
            float halfHeight = (_size * _maxAspectRatio) * 0.5f;

            for (int y = step / 2; y < bitmapData.Height; y += step)
            {
                int row = y * stride;
                for (int x = step / 2; x < bitmapData.Width; x += step)
                {
                    int alpha = pixels[row + (x * 4) + 3];
                    if (alpha < 70)
                    {
                        continue;
                    }

                    _textTargets.Add(new PointF(
                        placementArea.Left + x - halfWidth,
                        placementArea.Top + y - halfHeight));
                }
            }
        }
        finally
        {
            canvas.UnlockBits(bitmapData);
        }

        if (_textTargets.Count == 0)
        {
            return;
        }

        _textTargets.Sort(static (left, right) =>
        {
            int byY = left.Y.CompareTo(right.Y);
            return byY != 0 ? byY : left.X.CompareTo(right.X);
        });
        int mascotCount = _mascots.Count;
        var distributedTargets = new List<PointF>(mascotCount);
        if (_textTargets.Count >= mascotCount)
        {
            double step = _textTargets.Count / (double)mascotCount;
            for (int i = 0; i < mascotCount; i += 1)
            {
                int sourceIndex = Math.Min(_textTargets.Count - 1, (int)Math.Floor(i * step));
                distributedTargets.Add(ClampTargetToPlacementArea(_textTargets[sourceIndex], placementArea));
            }
        }
        else
        {
            int basePerTarget = mascotCount / _textTargets.Count;
            int remainder = mascotCount % _textTargets.Count;
            float spread = Math.Max(3f, _size * 0.22f);

            for (int i = 0; i < _textTargets.Count; i += 1)
            {
                PointF baseTarget = _textTargets[i];
                int countForTarget = basePerTarget + (i < remainder ? 1 : 0);
                for (int k = 0; k < countForTarget; k += 1)
                {
                    PointF point;
                    if (k == 0)
                    {
                        point = baseTarget;
                    }
                    else
                    {
                        // Spread extra mascots around each base point using a spiral pattern.
                        float angle = (float)((k - 1) * 2.3999632);
                        float radius = spread * MathF.Sqrt(k);
                        point = new PointF(
                            baseTarget.X + (MathF.Cos(angle) * radius),
                            baseTarget.Y + (MathF.Sin(angle) * radius));
                    }

                    distributedTargets.Add(ClampTargetToPlacementArea(point, placementArea));
                }
            }
        }

        if (distributedTargets.Count == 0)
        {
            return;
        }

        while (distributedTargets.Count < mascotCount)
        {
            PointF source = distributedTargets[_random.Next(distributedTargets.Count)];
            distributedTargets.Add(source);
        }

        if (distributedTargets.Count > mascotCount)
        {
            distributedTargets.RemoveRange(mascotCount, distributedTargets.Count - mascotCount);
        }

        _textTargets.Clear();
        _textTargets.AddRange(distributedTargets);

        int[] mascotOrder = new int[mascotCount];
        for (int i = 0; i < mascotOrder.Length; i += 1)
        {
            mascotOrder[i] = i;
        }

        Array.Sort(
            mascotOrder,
            (leftIndex, rightIndex) =>
            {
                MascotState left = _mascots[leftIndex];
                MascotState right = _mascots[rightIndex];
                int byY = left.Y.CompareTo(right.Y);
                return byY != 0 ? byY : left.X.CompareTo(right.X);
            });

        for (int i = 0; i < mascotOrder.Length; i += 1)
        {
            _textTargetByMascot[mascotOrder[i]] = i;
        }
    }

    private Rectangle ResolveTextPlacementArea(Size clientSize)
    {
        Rectangle clientRect = new(0, 0, Math.Max(1, clientSize.Width), Math.Max(1, clientSize.Height));
        if (_textPlacementAreas.Length == 0)
        {
            return clientRect;
        }

        var candidates = new List<Rectangle>(_textPlacementAreas.Length);
        for (int i = 0; i < _textPlacementAreas.Length; i += 1)
        {
            Rectangle clipped = Rectangle.Intersect(clientRect, _textPlacementAreas[i]);
            if (clipped.Width <= 0 || clipped.Height <= 0)
            {
                continue;
            }

            candidates.Add(clipped);
        }

        if (candidates.Count == 0)
        {
            return clientRect;
        }

        return candidates[_random.Next(candidates.Count)];
    }

    private PointF ClampTargetToPlacementArea(PointF target, Rectangle placementArea)
    {
        float minX = placementArea.Left;
        float minY = placementArea.Top;
        float maxX = Math.Max(minX, placementArea.Right - _size);
        float maxY = Math.Max(minY, placementArea.Bottom - (_size * _maxAspectRatio));
        return new PointF(
            Math.Clamp(target.X, minX, maxX),
            Math.Clamp(target.Y, minY, maxY));
    }

    private void BuildSpatialGrid(float cellSize)
    {
        ReturnAllLists();
        _grid.Clear();

        for (int i = 0; i < _mascots.Count; i += 1)
        {
            MascotState mascot = _mascots[i];
            int cellX = (int)MathF.Floor(mascot.X / cellSize);
            int cellY = (int)MathF.Floor(mascot.Y / cellSize);
            long key = MakeCellKey(cellX, cellY);

            if (!_grid.TryGetValue(key, out List<int>? indices))
            {
                indices = RentList();
                _grid[key] = indices;
            }

            indices.Add(i);
        }
    }

    private List<int> RentList()
    {
        if (_listPool.Count > 0)
        {
            var list = _listPool[_listPool.Count - 1];
            _listPool.RemoveAt(_listPool.Count - 1);
            return list;
        }

        return new List<int>(8);
    }

    private void ReturnAllLists()
    {
        foreach (var pair in _grid)
        {
            pair.Value.Clear();
            _listPool.Add(pair.Value);
        }
    }

    private static long MakeCellKey(int x, int y)
    {
        return ((long)x << 32) ^ (uint)y;
    }

    private bool ShouldResolveCollisions(Size clientSize)
    {
        if (_mascots.Count < 2)
        {
            return false;
        }

        int step = 1;
        long area = Math.Max(1L, (long)Math.Max(1, clientSize.Width) * Math.Max(1, clientSize.Height));
        long visualLoad = (long)_mascots.Count * _size * _size;

        if (_mascots.Count >= 1700 || visualLoad > area / 3)
        {
            step = 5;
        }
        else if (_mascots.Count >= 1300 || visualLoad > area / 4)
        {
            step = 4;
        }
        else if (_mascots.Count >= 900 || visualLoad > area / 6)
        {
            step = 3;
        }
        else if (_mascots.Count >= 450 || visualLoad > area / 10)
        {
            step = 2;
        }

        return _updateCount % step == 0;
    }

    private int ResolveMaxChecksPerMascot(Size clientSize)
    {
        long area = Math.Max(1L, (long)Math.Max(1, clientSize.Width) * Math.Max(1, clientSize.Height));
        long visualLoad = (long)_mascots.Count * _size * _size;

        if (_mascots.Count >= 1700 || visualLoad > area / 3)
        {
            return 12;
        }

        if (_mascots.Count >= 1300 || visualLoad > area / 4)
        {
            return 18;
        }

        if (_mascots.Count >= 900 || visualLoad > area / 6)
        {
            return 26;
        }

        if (_mascots.Count >= 450 || visualLoad > area / 10)
        {
            return 36;
        }

        return int.MaxValue;
    }

    private void ApplyRandomizedCruiseSpeed(MascotState mascot, float dt)
    {
        if (dt <= 0f)
        {
            return;
        }

        if (mascot.BaseSpeed <= 0.01f)
        {
            float initialSpeedSquared = (mascot.Vx * mascot.Vx) + (mascot.Vy * mascot.Vy);
            mascot.BaseSpeed = initialSpeedSquared > 0.01f ? MathF.Sqrt(initialSpeedSquared) : 45f;
        }

        mascot.SpeedRetargetTimer -= dt;
        if (mascot.SpeedRetargetTimer <= 0f)
        {
            mascot.SpeedRetargetTimer = 0.7f + (float)_random.NextDouble() * 2.4f;
            mascot.SpeedTargetFactor = SpeedTargetFactorMin + (float)_random.NextDouble() * (SpeedTargetFactorMax - SpeedTargetFactorMin);
        }

        float smooth = Math.Min(1f, dt * 1.8f);
        mascot.SpeedCurrentFactor += (mascot.SpeedTargetFactor - mascot.SpeedCurrentFactor) * smooth;

        float targetSpeed = Math.Max(16f, mascot.BaseSpeed * mascot.SpeedCurrentFactor);
        float speedSquared = (mascot.Vx * mascot.Vx) + (mascot.Vy * mascot.Vy);
        if (speedSquared < 0.0001f)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
            mascot.Vx = MathF.Cos(angle) * targetSpeed;
            mascot.Vy = MathF.Sin(angle) * targetSpeed;
            return;
        }

        float speed = MathF.Sqrt(speedSquared);
        float ratio = targetSpeed / speed;
        float maxDelta = Math.Min(0.28f, dt * 2.4f);
        float scale = 1f + Math.Clamp(ratio - 1f, -maxDelta, maxDelta);
        mascot.Vx *= scale;
        mascot.Vy *= scale;
    }

    private static Pose ResolvePose(float vx, float vy)
    {
        float absX = Math.Abs(vx);
        float absY = Math.Abs(vy);

        if (absX > absY)
        {
            return vx >= 0 ? Pose.Right : Pose.Left;
        }

        if (absY > 0.001f)
        {
            return vy >= 0 ? Pose.Down : Pose.Up;
        }

        return Pose.Idle;
    }

    private static float GetDirectionalRadius(float radiusX, float radiusY, float nx, float ny)
    {
        float safeRadiusX = Math.Max(1.0f, radiusX);
        float safeRadiusY = Math.Max(1.0f, radiusY);
        float denominator =
            (nx * nx) / (safeRadiusX * safeRadiusX) +
            (ny * ny) / (safeRadiusY * safeRadiusY);

        if (denominator <= 0.000001f)
        {
            return Math.Max(safeRadiusX, safeRadiusY);
        }

        return 1f / MathF.Sqrt(denominator);
    }

    private static float ImpactFromSpeed(float speed, float scale)
    {
        float baseValue = Math.Abs(speed) / scale;
        if (baseValue < 0.04f)
        {
            return 0f;
        }

        return Math.Min(1.6f, MathF.Pow(baseValue, 0.85f));
    }

    private static void ApplyWallSquish(MascotState mascot, bool isX, float impact)
    {
        if (impact <= 0f)
        {
            return;
        }

        float compress = 0.24f * impact;
        float expand = 0.16f * impact;

        if (isX)
        {
            mascot.SquishX = 1f - compress;
            mascot.SquishY = 1f + expand;
        }
        else
        {
            mascot.SquishX = 1f + expand;
            mascot.SquishY = 1f - compress;
        }

        mascot.SquishTimer = 0.12f;
    }

    private static PoseSprite[] LoadPoseSprites(int targetSize)
    {
        return
        [
            new PoseSprite(LoadImage("tachi.png"), targetSize),
            new PoseSprite(LoadImage("mae.png"), targetSize),
            new PoseSprite(LoadImage("migi.png"), targetSize),
            new PoseSprite(LoadImage("hidari.png"), targetSize),
            new PoseSprite(LoadImage("ushiro.png"), targetSize)
        ];
    }

    private static Image LoadImage(string fileName)
    {
        Assembly assembly = typeof(MascotSimulation).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"\u57cb\u3081\u8fbc\u307f\u753b\u50cf\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093: {fileName}");
        }

        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null)
        {
            throw new InvalidOperationException($"\u57cb\u3081\u8fbc\u307f\u753b\u50cf\u3092\u958b\u3051\u307e\u305b\u3093: {fileName}");
        }

        using var original = Image.FromStream(resourceStream);
        return new Bitmap(original);
    }

    private sealed class MascotState
    {
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public float BobPhase;
        public Pose Pose = Pose.Idle;
        public float SquishX = 1f;
        public float SquishY = 1f;
        public float SquishTimer;
        public float BaseSpeed;
        public float SpeedTargetFactor = 1f;
        public float SpeedCurrentFactor = 1f;
        public float SpeedRetargetTimer;
    }

    private enum FormationPhase
    {
        None,
        Gathering,
        Holding,
        Dispersing
    }

    private enum GiantMascotState
    {
        Hidden,
        Rising,
        Holding,
        Falling
    }

    private enum Pose
    {
        Idle,
        Down,
        Right,
        Left,
        Up
    }

    private sealed class PoseSprite : IDisposable
    {
        public Image Image { get; }
        public float AspectRatio { get; }
        public Bitmap Scaled { get; }
        public int ScaledWidth { get; }
        public int ScaledHeight { get; }
        public float CollisionCenterX { get; }
        public float CollisionCenterY { get; }
        public float CollisionRadiusX { get; }
        public float CollisionRadiusY { get; }
        public float CollisionMaxRadius { get; }

        public PoseSprite(Image image, int targetWidth)
        {
            Image = image;
            AspectRatio = image.Height / (float)Math.Max(1, image.Width);

            ScaledWidth = targetWidth;
            ScaledHeight = (int)MathF.Ceiling(targetWidth * AspectRatio);

            Scaled = new Bitmap(ScaledWidth, ScaledHeight, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(Scaled);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(image, 0, 0, ScaledWidth, ScaledHeight);

            CollisionShape shape = AnalyzeCollisionShape(Scaled);
            CollisionCenterX = shape.CenterX;
            CollisionCenterY = shape.CenterY;
            CollisionRadiusX = shape.RadiusX;
            CollisionRadiusY = shape.RadiusY;
            CollisionMaxRadius = shape.MaxRadius;
        }

        public void Dispose()
        {
            Scaled.Dispose();
            Image.Dispose();
        }

        public float GetDirectionalRadius(float nx, float ny)
        {
            float radiusX = Math.Max(1.0f, CollisionRadiusX);
            float radiusY = Math.Max(1.0f, CollisionRadiusY);
            float denominator = (nx * nx) / (radiusX * radiusX) + (ny * ny) / (radiusY * radiusY);
            if (denominator <= 0.000001f)
            {
                return CollisionMaxRadius;
            }

            return 1f / MathF.Sqrt(denominator);
        }

        private static CollisionShape AnalyzeCollisionShape(Bitmap bitmap)
        {
            const int alphaThreshold = 72;
            const float radiusXScale = 0.76f;
            const float radiusYScale = 0.86f;

            Rectangle rect = new(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                int stride = data.Stride;
                int width = data.Width;
                int height = data.Height;
                int byteCount = stride * height;
                byte[] pixels = GC.AllocateUninitializedArray<byte>(byteCount);
                Marshal.Copy(data.Scan0, pixels, 0, byteCount);

                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;

                for (int y = 0; y < height; y += 1)
                {
                    int row = y * stride;
                    for (int x = 0; x < width; x += 1)
                    {
                        int alpha = pixels[row + (x * 4) + 3];
                        if (alpha < alphaThreshold)
                        {
                            continue;
                        }

                        if (x < minX)
                        {
                            minX = x;
                        }

                        if (x > maxX)
                        {
                            maxX = x;
                        }

                        if (y < minY)
                        {
                            minY = y;
                        }

                        if (y > maxY)
                        {
                            maxY = y;
                        }
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    float fallbackCenterX = width * 0.5f;
                    float fallbackCenterY = height * 0.5f;
                    float fallbackRadiusX = Math.Max(1.5f, width * 0.24f);
                    float fallbackRadiusY = Math.Max(1.5f, height * 0.24f);
                    float fallbackMaxRadius = Math.Max(fallbackRadiusX, fallbackRadiusY);
                    return new CollisionShape(fallbackCenterX, fallbackCenterY, fallbackRadiusX, fallbackRadiusY, fallbackMaxRadius);
                }

                float centerX = (minX + maxX + 1) * 0.5f;
                float centerY = (minY + maxY + 1) * 0.5f;
                float maxAbsX = 0f;
                float maxAbsY = 0f;

                for (int y = minY; y <= maxY; y += 1)
                {
                    int row = y * stride;
                    for (int x = minX; x <= maxX; x += 1)
                    {
                        int alpha = pixels[row + (x * 4) + 3];
                        if (alpha < alphaThreshold)
                        {
                            continue;
                        }

                        float dx = (x + 0.5f) - centerX;
                        float dy = (y + 0.5f) - centerY;
                        float absX = Math.Abs(dx);
                        float absY = Math.Abs(dy);
                        if (absX > maxAbsX)
                        {
                            maxAbsX = absX;
                        }

                        if (absY > maxAbsY)
                        {
                            maxAbsY = absY;
                        }
                    }
                }

                float radiusX = MathF.Max(1.5f, (maxAbsX + 0.5f) * radiusXScale);
                float radiusY = MathF.Max(1.5f, (maxAbsY + 0.5f) * radiusYScale);
                float minRadiusX = MathF.Max(1.5f, width * 0.10f);
                float minRadiusY = MathF.Max(1.5f, height * 0.10f);
                if (radiusX < minRadiusX)
                {
                    radiusX = minRadiusX;
                }

                if (radiusY < minRadiusY)
                {
                    radiusY = minRadiusY;
                }

                float maxRadius = Math.Max(radiusX, radiusY);
                return new CollisionShape(centerX, centerY, radiusX, radiusY, maxRadius);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private readonly record struct CollisionShape(
            float CenterX,
            float CenterY,
            float RadiusX,
            float RadiusY,
            float MaxRadius);
    }

    public readonly record struct MascotDrawCommand(
        int PoseIndex,
        float X,
        float Y,
        float Width,
        float Height,
        bool UseLinearSampling);
}
