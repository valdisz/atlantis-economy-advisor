import { RegionFragment, StructureFragment, UnitFragment } from '../schema'
import {
    Ruleset, Region, Level, WorldInfo, Provinces, Structure, MovementPathfinder, ICoords, oppositeDirection, Factions, Unit,
    WorldLevel
} from './internal'

export class World {
    constructor(public readonly info: WorldInfo, public readonly ruleset: Ruleset) {
        for (let i = 0; i < info.map.length; i++) {
            this.addLevel(i, info.map[i])
        }
    }

    readonly levels: Level[] = []
    readonly provinces = new Provinces()
    readonly factions = new Factions()

    readonly pathfinder = new MovementPathfinder()

    private addLevel(z: number, { width, height, label }: WorldLevel) {
        const level = new Level(width, height, z, label)
        this.levels.push(level)

        return level
    }

    private linkRegions(regions: RegionFragment[]) {
        for (const reg of regions) {
            var source = this.getRegion(reg)

            for (const exit of reg.exits) {
                if (source.neighbors.has(exit.direction)) {
                    continue
                }

                let target = this.getRegion(exit)

                if (!target) {
                    target = Region.fromExit(exit, this.ruleset)

                    const province = this.provinces.getOrCreate(exit.province)
                    target.province = province
                    province.add(target)

                    let level: Level = this.getLevel(target.coords.z)
                    level.add(target);
                }

                source.neighbors.set(source, exit.direction, target)
                target.neighbors.set(target, oppositeDirection(exit.direction), source)
            }
        }
    }

    private addCoveredRegions() {
        for (const level of this.levels) {
            for (let x = 0; x < level.width; x++) {
                for (let y = 0; y < level.height; y++) {
                    if ((x + y) % 2) {
                        continue
                    }

                    if (level.get(x, y)) {
                        continue
                    }

                    level.add(Region.createCovered(x, y, level.index, level.label, this.ruleset))
                }
            }
        }
    }

    private linkCoveredRegions() {
        for (const level of this.levels) {
            for (const reg of level) {
                if (!reg || !reg.covered) {
                    continue
                }

                //
            }
        }
    }

    private linkProvinces() {
        for (const province of this.provinces) {
            const neighbors = new Set<string>()
            for (const reg of province.regions) {
                for (const exit of reg.neighbors) {
                    neighbors.add(exit.target.province.name)
                }
            }

            for (const name of neighbors) {
                if (name === province.name) continue

                const other = this.provinces.get(name)
                province.addBorderWith(other)
            }
        }
    }

    addFaction(num: number, name: string, isPlayer: boolean) {
        this.factions.create(num, name, isPlayer)
    }

    addRegions(regions: RegionFragment[]) {
        for (const reg of regions) {
            this.addRegion(reg);

            for (const str of reg.structures) {
                this.addStructure(str);
            }
        }

        this.linkRegions(regions)
        this.linkProvinces()
        this.addCoveredRegions()
        // this.linkCoveredRegions()
    }

    addUnits(units: UnitFragment[]) {
        for (const unit of units) {
            this.addUnit(unit);
        }
    }

    addRegion(region: RegionFragment) {
        const reg = Region.from(region, this.ruleset);

        const province = this.provinces.getOrCreate(region.province);
        province.add(reg);

        let level: Level = this.getLevel(reg.coords.z);
        level.add(reg);

        return reg;
    }

    addStructure(structure: StructureFragment) {
        const str = Structure.from(structure, this.ruleset)
        const region = this.getRegion(structure)

        region.addStructure(str)
    }

    addUnit(unit: UnitFragment) {
        const u = Unit.from(unit, this.factions, this.ruleset)
        const region = this.getRegion(unit)
        const structure = unit.structureNumber ? region.structures.find(x => x.num === unit.structureNumber) : null

        region.addUnit(u, structure)
    }


    getRegion(id: string): Region
    getRegion(coords: ICoords): Region
    getRegion(x: number, y: number, z: number): Region
    getRegion(multi: ICoords | number | string, y?: number, z?: number): Region {
        if (typeof(multi) === 'string') {
            for (const level of this.levels) {
                const region = level.getById(multi)
                if (region) return region
            }

            return null
        }

        const level = this.getLevel(typeof(multi) === 'number' ? z : multi.z)
        if (!level) return null

        return typeof(multi) === 'number'
            ? level.get(multi, y)
            : level.get(multi)
    }

    getLevel(z: number): Level {
        return this.levels[z]
    }
}
