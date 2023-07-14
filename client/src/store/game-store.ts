import { ApolloQueryResult } from '@apollo/client'
import { makeObservable, observable, IObservableArray, runInAction, action, computed, reaction, comparer, transaction, autorun } from 'mobx'
import { CLIENT } from '../client'
import { RegionFragment, UnitFragment, Stance } from '../schema'
import { GetGame, GetGameQuery, GetGameQueryVariables } from '../schema'
import { GetTurn, GetTurnQuery, GetTurnQueryVariables } from '../schema'
import { GetRegions, GetRegionsQuery, GetRegionsQueryVariables } from '../schema'
import { GetUnits, GetUnitsQuery, GetUnitsQueryVariables } from '../schema'
import { SetOrder, SetOrderMutation, SetOrderMutationVariables } from '../schema'
import { OrdersGet, OrdersGetQuery, OrdersGetQueryVariables } from '../schema'
import { Ruleset, SkillInfo } from "../game"
import { Region } from "../game"
import { World } from "../game"
import { WorldInfo, WorldLevel } from '../game'
import { Unit } from '../game'
import { saveAs } from 'file-saver'
import { InterfaceCommand, MoveCommand } from './commands/move'
import { UniversityStore } from './university-store'
import { Paths } from '../map'
import { mutate } from './connection'

export class TurnsStore {
    constructor() {
        makeObservable(this)
    }
}

interface Progress {
    total: number
    position: number
}

interface ProgressCallback {
    (progress: Progress): void
}

export type OrdersState = 'SAVED' | 'UNSAVED' | 'SAVING' | 'ERROR'

export class GameLoadingStore {
    constructor() {
        makeObservable(this)
    }

    @observable isLoading = true

    readonly done: IObservableArray<string> = observable([])

    @observable phase: string = ''

    @computed get indeterminate() {
        return this.total === 0
    }

    @observable value = 0
    @observable total = 0

    @computed get progress() {
        return this.total === 0
            ? 0
            : this.value / this.total * 100
    }

    @action begin(name: string) {
        this.end()

        this.phase = name
        this.total = 0
        this.isLoading = true
    }

    @action end() {
        if (!this.phase) {
            return
        }

        this.done.push(this.phase)
        this.phase = null
        this.isLoading = false
    }

    @action update(value: number, total: number) {
        this.value = Math.min(value, total)
        this.total = total
    }

    @action clear() {
        this.done.clear()
        this.isLoading = true
        this.value = 0
        this.total = 0
        this.phase = ''
    }
}

interface UnitOrders {
    unitNumber: number
    orders?: string
}

export class GameStore {
    constructor() {
        makeObservable(this)

        reaction(
            () => ({ unit: this.unit, orders: this.unitOrders }),
            async ({ unit, orders }, { unit: prevUnit, orders: prevOrders }) => {
                if (!unit || !prevUnit || unit.num !== prevUnit.num) return

                this.startOrdersSaving()

                if (this.ordersSaveAbortController) {
                    this.ordersSaveAbortController.abort()
                }
                this.ordersSaveAbortController = new AbortController()

                // FIXME    
                // try {
                //     const response = await mutate<SetOrderMutation, SetOrderMutationVariables>(SetOrder, { unitId: unit.id, orders })
                //     const result = response.data?.setOrders
                //     if (!response.error && result?.isSuccess) {
                //         unit.setOrders(orders)
                //         this.setPaths([ unit.path ])
                //     }
                //     else {
                //         this.setOrders(prevOrders)
                //         this.errorOrdersSaving()
                //     }

                //     this.stopOrdersSaving()
                // }
                // catch {
                //     this.errorOrdersSaving()
                // }
            },
            {
                equals: comparer.shallow,
                delay: 1000
            }
        )

        this.commands.push(new MoveCommand(this))
    }

    readonly commands: InterfaceCommand[] = [ ]

    private ordersSaveAbortController: AbortController

    university: UniversityStore = null

    gameId: string = null
    playerId: string = null

    @observable world: World = null

    @observable name: string = null
    factionNumber: number = null

    @computed get unclaimed() {
        return this.world.unclaimed || 0
    }

    get faction() {
        return this.world.getFaction(this.factionNumber)
    }

    @computed get money() {
        return this.faction.troops.reduce((acc, value) => acc + value.money, 0)
    }

    @computed get men() {
        return this.faction.troops.reduce((acc, value) => acc + value.inventory.menCount, 0)
    }

    @computed get weapons() {
        return this.faction.troops.reduce((acc, value) => acc + value.inventory.items.reduce((ac, v) => ac + (v.info.category === 'weapon' ? v.amount : 0), 0), 0)
    }

    @computed get food() {
        return this.faction.troops.reduce((acc, value) => acc + value.inventory.items.reduce((ac, v) => ac + (v.info.category === 'food' ? v.amount : 0), 0), 0)
    }

    @computed get mounts() {
        return this.faction.troops.reduce((acc, value) => acc + value.inventory.items.reduce((ac, v) => ac + (v.info.category === 'mount' ? v.amount : 0), 0), 0)
    }

    async loadRegions(turnId: string, onProgress: ProgressCallback) {
        const items: RegionFragment[] = []

        let skip = 0
        let response: ApolloQueryResult<GetRegionsQuery> = null
        do {
            response = await CLIENT.query<GetRegionsQuery, GetRegionsQueryVariables>({
                query: GetRegions,
                variables: { skip, turnId, pageSize: 1000 },
            })

            if (response.data.node.__typename !== 'PlayerTurn') {
                return items
            }

            const data = response.data.node
            data.regions.items.forEach(x => items.push(x))

            onProgress({ total: data.regions.totalCount, position: items.length })
            skip = items.length
        }
        while (response.data.node.regions.pageInfo.hasNextPage)

        return items
    }

    async loadUnits(turnId: string, onProgress: ProgressCallback) {
        const items: UnitFragment[] = []

        let skip = 0
        let response: ApolloQueryResult<GetUnitsQuery> = null
        do {
            response = await CLIENT.query<GetUnitsQuery, GetUnitsQueryVariables>({
                query: GetUnits,
                variables: { skip, turnId, pageSize: 1000 }
            })

            if (response.data.node.__typename !== 'PlayerTurn') {
                return items
            }

            const data = response.data.node
            data.units.items.forEach(x => items.push(x))

            onProgress({ total: data.units.totalCount, position: items.length })
            skip = items.length
        }
        while (response.data.node.units.pageInfo.hasNextPage)

        return items
    }

    async loadOrders(turnId: string, onProgress: ProgressCallback) {
        const items: UnitOrders[] = []

        let skip = 0
        let response: ApolloQueryResult<OrdersGetQuery> = null
        do {
            response = await CLIENT.query<OrdersGetQuery, OrdersGetQueryVariables>({
                query: OrdersGet,
                variables: { skip, turnId, take: 100 }
            })

            if (response.data.node.__typename !== 'PlayerTurn') {
                return items
            }

            const data = response.data.node
            data.orders.items.forEach(x => items.push(x))

            onProgress({ total: data.orders.totalCount, position: items.length })
            skip = items.length
        }
        while (response.data.node.orders.pageInfo.hasNextPage)

        return items
    }

    async load(gameId: string, loading: GameLoadingStore) {
        if (this.gameId === gameId) {
            return
        }
        this.gameId = gameId

        loading.begin('Game information')

        const response = await CLIENT.query<GetGameQuery, GetGameQueryVariables>({
            query: GetGame,
            variables: {
                gameId
            }
        })

        if (response.data.node.__typename !== 'Game') {
            return
        }

        const { me, ...game } = response.data.node
        this.playerId = me.id
        this.factionNumber = me.number

        runInAction(() => {
            this.name = game.name
        })

        loading.begin(`Turn ${me.lastTurnNumber} information`)

        const turnDetails = await CLIENT.query<GetTurnQuery, GetTurnQueryVariables>({
            query: GetTurn,
            variables: {
                turnId: me.lastTurn.id
            }
        })

        if (turnDetails.data.node.__typename !== 'PlayerTurn') {
            return
        }

        const turn = turnDetails.data.node

        const levels = [...game.options.map]
        levels.sort((a, b) => a.level - b.level)

        const map: WorldLevel[] = levels.map(level => ({
            label: level.label,
            width: level.width,
            height: level.height
        }))
        const worldInfo: WorldInfo = { map }

        const ruleset = new Ruleset()
        // FIXME
        // ruleset.parse(game.ruleset)

        const world = new World(worldInfo, ruleset)
        world.turnNumber = turn.turnNumber
        world.month = turn.turnNumber % 12
        world.year = Math.floor(turn.turnNumber / 12) + 1
        world.unclaimed = turn.unclaimed || 0

        const attitudes = new Map<number, Stance>()
        let defaultAttitude = Stance.Neutral
        for (const faction of turn.factions) {
            const isPlayer = faction.number === this.factionNumber
            world.addFaction(faction.number, faction.name, isPlayer)

            if (isPlayer) {
                defaultAttitude = faction.defaultAttitude
                for (const att of faction.attitudes) {
                    attitudes.set(att.factionNumber, att.stance)
                }
            }
        }

        world.setAttitudes(defaultAttitude, attitudes)

        loading.begin(`Regions`)
        const regions = await this.loadRegions(turn.id, ({ position, total }) => loading.update(position, total))
        world.addRegions(regions)

        loading.begin(`Units`)
        const units = await this.loadUnits(turn.id, ({ position, total }) => loading.update(position, total))
        world.addUnits(units)

        loading.begin('Orders')
        const unitOrders = await this.loadOrders(turn.id, ({ position, total }) => loading.update(position, total))

        for (const { unitNumber, orders } of unitOrders) {
            const unit = world.getUnit(unitNumber)
            if (unit) {
                unit.setOrders(orders)
            }
        }

        for (const level of world.levels) {
            for (const region of level) {
                region.sort()
            }
        }

        for (const battle of turn.battles) {
            world.addBattle(battle)
        }

        this.university = new UniversityStore(world)
        await this.university.load(this.gameId, turn.turnNumber)

        world.findAllTradeRoutes()

        runInAction(() =>{
            this.world = world
        })
    }

    @observable region: Region = null
    @observable paths: Paths

    @action selectRegion = (reg: Region) => {
        if (!this.region) {
            this.region = observable(reg)
            return
        }

        const { x: x1, y: y1, z: z1 } = reg.coords
        const { x: x2, y: y2, z: z2 } = this.region.coords

        if (x1 !== x2 || y1 !== y2 || z1 !== z2) {
            this.region = observable(reg)
            this.unit = null
            this.setPaths([]);
        }

        if (!(reg?.covered ?? true)) {
            window.localStorage.setItem('coords', JSON.stringify(reg.coords))
        }
    }

    @computed get units() {
        const units = this.region?.units ?? []
        return units
    }

    @computed get structures() {
        const structures = this.region?.structures ?? []
        return structures
    }

    @observable unit: Unit = null
    @observable unitOrders: string = ''
    @observable ordersState: OrdersState = 'SAVED'
    ordersChanged = false

    @computed get isOrdersVisible() {
        return this.unit?.isPlayer || this.unit?.orders?.length > 0
    }

    @computed get isOrdersReadonly() {
        return !(this.unit?.isPlayer ?? false)
    }

    @action selectUnit = (unit: Unit) => {
        this.unit = unit
        this.unitOrders = unit?.ordersSrc
        this.ordersState = 'SAVED'
        this.setPaths([this.unit.path]);
    }

    @action setOrders = (orders: string) => {
        const changed = this.unitOrders !== orders
        this.unitOrders = orders

        if (changed) {
            this.ordersState = 'UNSAVED'
            this.ordersChanged = true
        }
    }

    @action setPaths = (paths: Paths) => {
        this.paths = paths;
    }

    @action startOrdersSaving = () => this.ordersState = 'SAVING'
    @action stopOrdersSaving = () => this.ordersState = 'SAVED'
    @action errorOrdersSaving = () => this.ordersState = 'ERROR'

    @observable battleSimOpen = false
    readonly attackers: IObservableArray<Unit> = observable([])
    readonly defenders: IObservableArray<Unit> = observable([])

    @action addAttacker = (unit: Unit) => this.attackers.push(unit)
    @action addDefender = (unit: Unit) => this.defenders.push(unit)
    @action removeFromBattleSim = (unit: Unit) => this.defenders.remove(unit) || this.attackers.remove(unit)
    @action resetBattleSim = () => {
        this.attackers.clear()
        this.defenders.clear()
    }

    @action openBattleSim = () => {
        this.resetBattleSim()
        this.battleSimOpen = true
    }

    @action closeBattleSim = () => {
        this.resetBattleSim()
        this.battleSimOpen = false
    }

    isAttacker = (unit: Unit) => this.attackers.some(x => x.id === unit.id)
    isDefender = (unit: Unit) => this.defenders.some(x => x.id === unit.id)

    toBattleSim = () => {
        function toBattleSimUnit(unit: Unit) {
            const u: BattleSimUnit = {
                name: `${unit.name} - ${unit.num}`,
                skills: unit.skills.map(s => ({
                    abbr: s.code,
                    level: s.level
                })),
                items: unit.inventory.items
                    .map(i => ({
                        abbr: i.code,
                        amount: i.amount
                    })),
                combatSpell: unit.combatSpell?.code,
                flags: unit.flags.filter(x => x === 'behind') as any[]
            }

            // if (!u.skills.length) delete u.skills
            if (!u.items.length) delete u.items
            if (!u.flags.length) delete u.flags
            if (!u.combatSpell) delete u.combatSpell

            return u
        }

        const sim: BattleSim = {
            attackers: {
                units: this.attackers.map(toBattleSimUnit)
            },
            defenders: {
                units: this.defenders.map(toBattleSimUnit)
            }
        }

        const json = JSON.stringify(sim)

        const blob = new Blob([json], { type: 'application/json' })
        saveAs(blob, 'battlesim.json')
    }

    getOrders = () => {
        const lines = [
            '#atlantis ""'
        ]

        var faction = this.world.factions.get(this.factionNumber)
        for (const unit of faction.troops) {
            lines.push(`unit ${unit.num}`)
            lines.push(unit.ordersSrc)
            lines.push('')
        }

        lines.push('#end')
        lines.push('')

        const orders = lines.join('\n')
        const blob = new Blob([ orders ], { type: 'text/plain' })
        saveAs(blob, `orders-${this.world.turnNumber}.ord`)
    }

    @observable unitsPanel: boolean = true
    @observable regionPanel: boolean = true
    @observable structuresPanel: boolean = true
    @observable battlesVisible: boolean = false

    @action exapandUnits = (expand: boolean) => this.unitsPanel = expand
    @action exapandRegion = (expand: boolean) => this.regionPanel = expand
    @action exapandStructures = (expand: boolean) => this.structuresPanel = expand

    @action showBattles = () => this.battlesVisible = true
    @action hideBattles = () => this.battlesVisible = false
    toggleBattles = () => {
        if (this.battlesVisible) {
            this.hideBattles()
        }
        else {
            this.showBattles()
        }
    }
}

export interface BattleSimItem {
    abbr: string;
    amount: number;
}

export interface BattleSimUnit {
    name: string;
    items: BattleSimItem[];
    skills: BattleSimSkill[];
    flags: ('behind')[];
    combatSpell: string;
}

export interface BattleSimSkill {
    abbr: string;
    level: number;
}

export interface BattleSimUnitList {
    units: BattleSimUnit[]
}

export interface BattleSim {
    attackers: BattleSimUnitList
    defenders: BattleSimUnitList
}
