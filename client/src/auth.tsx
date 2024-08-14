import React from 'react'
import { CLIENT } from './client'
import { SignIn } from './components'
import { ApolloError } from '@apollo/client'
import { GetMe, GetMeQuery, GetMeQueryVariables } from './schema'
import { canUsePasskey } from './lib'

export enum Role {
    Root = 'root',
    GameMaster = 'game-master',
    UserManager = 'user-manager'
}

export class Auth {
    constructor (private readonly roles: Role[]) {

    }

    hasRole(role: Role) {
        return this.roles.includes(role)
    }
}

const rolesContext = React.createContext<Auth>(null)

export function useAuth() {
    return React.useContext(rolesContext)
}

export interface AuthenticateProps {
}

type AuthState = 'loading' | 'sign-in' | 'sign-in-passkey' | 'error' | 'normal'

export function Authenticate({ children }: React.PropsWithChildren<AuthenticateProps>) {
    const [ state, setState ] = React.useState<AuthState>('loading')
    const [ auth, setAuth ] = React.useState<Auth>(null)

    React.useEffect(() => {
        CLIENT.query<GetMeQuery, GetMeQueryVariables>({
            query: GetMe,
            errorPolicy: 'none'
        })
        .then(
            me => {
                setAuth(new Auth((me.data.me.roles || []) as any))
                return 'normal'
            },
            (err: ApolloError) => {
                const is401 = (err.networkError as any)?.statusCode === 401
                const isNotAuthorized = err.graphQLErrors
                    ?.map(x => x.extensions['code'])
                    ?.some(x => x === 'AUTH_NOT_AUTHORIZED' || x === 'AUTH_NOT_AUTHENTICATED')

                return is401 || isNotAuthorized ? 'sign-in' : 'error'
            }
        )
        .then(nextState => {
            if (nextState === 'sign-in') {
                return canUsePasskey().then(can => {
                    return can ? 'sign-in-passkey' : 'sign-in'
                })
            }

            return Promise.resolve(nextState)
        })
        .then(setState as any)
    }, [])

    const handleSignIn = (id: string, roles: string[]) => {
        setAuth(new Auth((roles || []) as any))
        setState('normal')
    }

    // canUsePasskey()

    if (state === 'loading') return (
        <div className="splash">
            <div className="lds-ring"><div></div><div></div><div></div><div></div></div>
        </div>
    )

    if (state.startsWith('sign-in')) return <SignIn withPasskey={
        false // TODO: implement passkey on backend before enabling here
        // state === 'sign-in-passkey'
    } onSuccess={handleSignIn} />

    if (state === 'error') return <div>Cannot reach server.</div>

    return <rolesContext.Provider value={auth}>{children}</rolesContext.Provider>
}

export interface ForRoleProps {
    role: Role | Role[]
    children: React.ReactNode
    forbidden?: React.ReactNode
}

export function ForRole({ role, children, forbidden }: ForRoleProps) {
    const auth = useAuth()
    if (!auth) {
        return <>{forbidden}</>
    }

    const isRoot = auth.hasRole(Role.Root)

    const isInRole = typeof role === 'string'
        ? auth.hasRole(role)
        : role.every(r => auth.hasRole(r))

    return <>{isRoot || isInRole ? children : (forbidden)}</>
}

export function forRole<P>(BaseComponent: React.ComponentType<P>, role: Role | Role[], forbidden?: React.ReactNode) {
    return (props) => <ForRole role={role} forbidden={forbidden}>
        <BaseComponent {...props} />
    </ForRole>
}
