// ════════════════════════════════════════════════════════════════
// COLYSEUS SERVER CODE STRUCTURE
// ════════════════════════════════════════════════════════════════
// This is your authoritative game server that runs all gameplay logic.
// Place this in your Colyseus server project (Node.js + TypeScript).
// ════════════════════════════════════════════════════════════════

import { Room, Client } from "@colyseus/core";

// ─────────────────────────────────────────────────────────────────
// 1. ENTITY TYPES (matches Unity enums)
// ─────────────────────────────────────────────────────────────────

enum EntityTeam {
  Team1 = 0,
  Team2 = 1,
}

interface EntityStats {
  maxHP: number;
  currentHP: number;
  moveSpeed: number;
  attackRange: number;
  attackDamage: number;
  attackSpeed: number;
  aggroRange: number;
}

interface Entity {
  id: number;
  x: number;
  y: number;
  team: EntityTeam;
  type: string; // "knight", "tower", etc.
  stats: EntityStats;
  target: Entity | null;
  isAlive: boolean;
  attackCooldown: number;
  isBuilding: boolean;
}

// ─────────────────────────────────────────────────────────────────
// 2. GAMEPLAY DIRECTOR (Core Simulation)
// ─────────────────────────────────────────────────────────────────

class GameplayDirector {
  private entities: Entity[] = [];
  private nextEntityId = 1;

  // Tick all entities
  update(dt: number): void {
    for (const entity of this.entities) {
      if (!entity.isAlive) continue;

      // Update movement
      this.updateMovement(entity, dt);

      // Update combat
      this.updateCombat(entity, dt);
    }

    // Remove dead entities
    this.entities = this.entities.filter((e) => e.isAlive);
  }

  private updateMovement(entity: Entity, dt: number): void {
    if (entity.isBuilding) return; // Buildings don't move

    // Try to acquire target if none
    if (!entity.target || !entity.target.isAlive) {
      entity.target = this.acquireTarget(entity);
    }

    // Stop if in attack range
    if (
      entity.target &&
      this.isInRange(entity, entity.target, entity.stats.attackRange)
    ) {
      return;
    }

    // Move forward or toward target
    let dirX = 0;
    let dirY = 0;

    if (entity.target && entity.target.isAlive) {
      // Move toward target
      dirX = entity.target.x - entity.x;
      dirY = entity.target.y - entity.y;
      const dist = Math.sqrt(dirX * dirX + dirY * dirY);
      if (dist > 0) {
        dirX /= dist;
        dirY /= dist;
      }
    } else {
      // Move forward along lane (Team1 goes +Y, Team2 goes -Y)
      dirY = entity.team === EntityTeam.Team1 ? 1 : -1;
    }

    entity.x += dirX * entity.stats.moveSpeed * dt;
    entity.y += dirY * entity.stats.moveSpeed * dt;
  }

  private updateCombat(entity: Entity, dt: number): void {
    if (!entity.target || !entity.target.isAlive) return;

    // Check if in attack range
    if (!this.isInRange(entity, entity.target, entity.stats.attackRange)) {
      return;
    }

    // Update attack cooldown
    entity.attackCooldown -= dt;
    if (entity.attackCooldown <= 0) {
      // Attack!
      this.dealDamage(entity, entity.target);
      entity.attackCooldown = 1.0 / entity.stats.attackSpeed;
    }
  }

  private acquireTarget(entity: Entity): Entity | null {
    let closest: Entity | null = null;
    let closestDist = entity.stats.aggroRange * entity.stats.aggroRange;

    for (const other of this.entities) {
      if (!other.isAlive || other.team === entity.team) continue;

      const distSq = (other.x - entity.x) ** 2 + (other.y - entity.y) ** 2;
      if (distSq < closestDist) {
        closest = other;
        closestDist = distSq;
      }
    }

    return closest;
  }

  private isInRange(entity: Entity, target: Entity, range: number): boolean {
    const distSq = (target.x - entity.x) ** 2 + (target.y - entity.y) ** 2;
    return distSq <= range * range;
  }

  private dealDamage(attacker: Entity, target: Entity): void {
    target.stats.currentHP -= attacker.stats.attackDamage;
    if (target.stats.currentHP <= 0) {
      target.stats.currentHP = 0;
      target.isAlive = false;
      console.log(`[Server] Entity ${target.id} (${target.type}) died`);
    }
  }

  // Spawn a new entity
  spawnEntity(
    type: string,
    x: number,
    y: number,
    team: EntityTeam,
    isBuilding = false
  ): Entity {
    const stats = this.getStatsForType(type);
    const entity: Entity = {
      id: this.nextEntityId++,
      x,
      y,
      team,
      type,
      stats,
      target: null,
      isAlive: true,
      attackCooldown: 0,
      isBuilding,
    };

    this.entities.push(entity);
    console.log(`[Server] Spawned ${type} (id=${entity.id}) at (${x}, ${y})`);
    return entity;
  }

  private getStatsForType(type: string): EntityStats {
    // TODO: Load from config files
    const configs: Record<string, EntityStats> = {
      knight: {
        maxHP: 100,
        currentHP: 100,
        moveSpeed: 2,
        attackRange: 1.5,
        attackDamage: 20,
        attackSpeed: 1.5,
        aggroRange: 5,
      },
      tower: {
        maxHP: 500,
        currentHP: 500,
        moveSpeed: 0,
        attackRange: 6,
        attackDamage: 50,
        attackSpeed: 1,
        aggroRange: 6,
      },
    };

    return configs[type] || configs.knight;
  }

  getEntities(): Entity[] {
    return this.entities;
  }
}

// ─────────────────────────────────────────────────────────────────
// 3. AI CONTROLLER
// ─────────────────────────────────────────────────────────────────

class AIController {
  private spawnCooldown = 0;
  private spawnInterval = 5; // seconds

  update(dt: number, director: GameplayDirector): void {
    this.spawnCooldown -= dt;
    if (this.spawnCooldown <= 0) {
      // Spawn AI unit
      director.spawnEntity("knight", 0, 15, EntityTeam.Team2);
      this.spawnCooldown = this.spawnInterval;
    }
  }
}

// ─────────────────────────────────────────────────────────────────
// 4. MATCH END DETECTOR
// ─────────────────────────────────────────────────────────────────

class MatchEndDetector {
  private matchOver = false;
  private winner: EntityTeam | null = null;

  update(director: GameplayDirector): void {
    if (this.matchOver) return;

    const entities = director.getEntities();
    const team1Towers = entities.filter(
      (e) => e.team === EntityTeam.Team1 && e.type === "tower" && e.isAlive
    );
    const team2Towers = entities.filter(
      (e) => e.team === EntityTeam.Team2 && e.type === "tower" && e.isAlive
    );

    if (team1Towers.length === 0) {
      this.matchOver = true;
      this.winner = EntityTeam.Team2;
      console.log("[Server] Match Over! Team2 Wins!");
    } else if (team2Towers.length === 0) {
      this.matchOver = true;
      this.winner = EntityTeam.Team1;
      console.log("[Server] Match Over! Team1 Wins!");
    }
  }

  isMatchOver(): boolean {
    return this.matchOver;
  }

  getWinner(): EntityTeam | null {
    return this.winner;
  }
}

// ═════════════════════════════════════════════════════════════════
// 5. COLYSEUS ROOM (Main Server Entry Point)
// ═════════════════════════════════════════════════════════════════

export class ClashRoom extends Room {
  private director!: GameplayDirector;
  private ai!: AIController;
  private matchEnd!: MatchEndDetector;

  onCreate(options: any) {
    console.log("[Server] ClashRoom created!");

    // Initialize gameplay systems
    this.director = new GameplayDirector();
    this.ai = new AIController();
    this.matchEnd = new MatchEndDetector();

    // Spawn initial towers (buildings)
    this.spawnInitialBuildings();

    // Listen to client messages
    this.onMessage("spawn-unit", (client, message) => {
      this.handleSpawnUnit(client, message);
    });

    // Start simulation loop (20 ticks per second)
    this.setSimulationInterval((dt) => this.onSimulationTick(dt), 50);
  }

  private spawnInitialBuildings(): void {
    // Team1 towers
    this.director.spawnEntity("tower", -5.5, -3, EntityTeam.Team1, true);
    this.director.spawnEntity("tower", 5.5, -3, EntityTeam.Team1, true);
    this.director.spawnEntity("tower", 0, -6.5, EntityTeam.Team1, true);

    // Team2 towers
    this.director.spawnEntity("tower", 0, 17.5, EntityTeam.Team2, true);
    this.director.spawnEntity("tower", -5.5, 15, EntityTeam.Team2, true);
    this.director.spawnEntity("tower", 5.5, 15, EntityTeam.Team2, true);
  }

  private handleSpawnUnit(client: Client, message: any): void {
    const { x, y, unitType } = message;
    console.log(
      `[Server] Client ${client.sessionId} wants to spawn ${unitType} at (${x}, ${y})`
    );

    // TODO: Validate spawn position, check player resources, etc.

    // Spawn the unit
    this.director.spawnEntity(unitType, x, y, EntityTeam.Team1);
  }

  private onSimulationTick(dt: number): void {
    // Update all gameplay systems
    this.director.update(dt);
    this.ai.update(dt, this.director);
    this.matchEnd.update(this.director);

    // Broadcast game state to all clients
    this.broadcastGameState();

    // Check if match is over
    if (this.matchEnd.isMatchOver()) {
      this.broadcast("match-over", { winner: this.matchEnd.getWinner() });
    }
  }

  private broadcastGameState(): void {
    const entities = this.director.getEntities().map((e) => ({
      id: e.id,
      x: e.x,
      y: e.y,
      hp: e.stats.currentHP,
      maxHp: e.stats.maxHP,
      type: e.type,
      team: e.team,
      isAlive: e.isAlive,
    }));

    // Send to all connected clients
    this.broadcast("entity-update", entities);
  }

  onJoin(client: Client, options: any) {
    console.log(`[Server] Client ${client.sessionId} joined!`);
  }

  onLeave(client: Client, consented: boolean) {
    console.log(`[Server] Client ${client.sessionId} left!`);
  }

  onDispose() {
    console.log("[Server] ClashRoom disposed!");
  }
}
